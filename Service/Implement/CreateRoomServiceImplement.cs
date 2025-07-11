using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Mapper.Rooms;
using ConsoleApp1.Mapper;

namespace ConsoleApp1.Service.Implement;

public class CreateRoomServiceImplement : ICreateRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomSettingsRepository _roomSettingsRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;

    public CreateRoomServiceImplement(
        IRoomRepository roomRepository,
        IRoomSettingsRepository roomSettingsRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository)
    {
        _roomRepository = roomRepository;
        _roomSettingsRepository = roomSettingsRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
    }

    public async Task<RoomDTO> CreateRoomAsync(CreateRoomRequest request, int userId)
    {
        Console.WriteLine($"[CREATE_ROOM_SERVICE] Creating room for userId: {userId}");
        
        // Kiểm tra user đã ở trong phòng nào chưa
        var existingRoom = await _roomPlayerRepository.GetActiveRoomByUserIdAsync(userId);
        if (existingRoom != null)
        {
            throw new InvalidOperationException($"Bạn đang ở trong phòng {existingRoom.RoomCode}. Vui lòng rời phòng trước khi tạo phòng mới.");
        }
        
        var roomCode = await GenerateUniqueRoomCodeAsync();
        
        var room = new Room
        {
            RoomCode = roomCode,
            RoomName = request.Name,
            IsPrivate = request.IsPrivate,
            OwnerId = userId,
            Status = "waiting",
            MaxPlayers = request.MaxPlayers,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var roomId = await _roomRepository.AddAsync(room);
        room.Id = roomId;

        await SetupRoomSettingsAsync(roomId, request);
        await UpdateUserTypeAccountAsync(room.OwnerId);
        
        // Tự động thêm owner vào phòng như một player
        var roomPlayer = new RoomPlayer
        {
            RoomId = roomId,
            UserId = userId,
            Score = 0,
            TimeTaken = TimeSpan.Zero,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _roomPlayerRepository.AddAsync(roomPlayer);
        
        Console.WriteLine($"[CREATE_ROOM_SERVICE] Room {roomCode} created successfully with owner {userId}");
        return RoomMapper.ToDTO(room);
    }

    public async Task<bool> UpdateRoomSettingsAsync(int roomId, RoomSetting settings)
    {
        var existingSetting = await _roomSettingsRepository.GetSettingAsync(roomId, settings.SettingKey);
        if (existingSetting != null)
        {
            existingSetting.SettingValue = settings.SettingValue;
            await _roomSettingsRepository.UpdateSettingAsync(existingSetting);
        }
        else
        {
            settings.RoomId = roomId;
            await _roomSettingsRepository.AddSettingAsync(settings);
        }
        return true;
    }

    public async Task<bool> KickPlayerAsync(int roomId, int playerId)
    {
        return await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, roomId);
    }

    public async Task<bool> UpdateRoomStatusAsync(int roomId, string status)
    {
        await _roomRepository.UpdateStatusAsync(roomId, status);
        return true;
    }

    public async Task<bool> SetTopicForRoomAsync(int roomId, int topicId)
    {
        var setting = new RoomSetting(roomId, "topic_id", topicId.ToString());
        return await UpdateRoomSettingsAsync(roomId, setting);
    }

    public async Task<bool> SetQuestionCountAsync(int roomId, int count)
    {
        var setting = new RoomSetting(roomId, "question_count", count.ToString());
        return await UpdateRoomSettingsAsync(roomId, setting);
    }

    public async Task<bool> DeleteRoomAsync(int roomId)
    {
        await _roomSettingsRepository.DeleteAllSettingsAsync(roomId);
        return await _roomRepository.DeleteAsync(roomId);
    }

    public async Task<bool> SetCountdownTimeAsync(int roomId, int seconds)
    {
        var setting = new RoomSetting(roomId, "countdown_seconds", seconds.ToString());
        return await UpdateRoomSettingsAsync(roomId, setting);
    }

    public async Task<bool> UpdateRoomPrivacyAsync(int roomId, bool isPrivate)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        room.IsPrivate = isPrivate;
        room.UpdatedAt = DateTime.UtcNow;
        await _roomRepository.UpdateAsync(room);
        return true;
    }

    public async Task<bool> SetGameModeAsync(int roomId, string gameMode)
    {
        if (gameMode != "1vs1" && gameMode != "battle") return false;
        
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        if (gameMode == "1vs1" && room.MaxPlayers != 2)
        {
            room.MaxPlayers = 2;
            await _roomRepository.UpdateAsync(room);
        }
        else if (gameMode == "battle" && room.MaxPlayers < 3)
        {
            room.MaxPlayers = 10;
            await _roomRepository.UpdateAsync(room);
        }

        var setting = new RoomSetting(roomId, "game_mode", gameMode);
        return await UpdateRoomSettingsAsync(roomId, setting);
    }

    public async Task<bool> UpdateMaxPlayersAsync(int roomId, int maxPlayers)
    {
        var gameModeSetting = await _roomSettingsRepository.GetSettingAsync(roomId, "game_mode");
        var gameMode = gameModeSetting?.SettingValue ?? "battle";
        
        if (gameMode == "1vs1" && maxPlayers != 2) return false;
        if (gameMode == "battle" && maxPlayers < 3) return false;

        await _roomRepository.UpdateMaxPlayersAsync(roomId, maxPlayers);
        return true;
    }

    public async Task<bool> TransferOwnershipAsync(int roomId, int newOwnerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        room.OwnerId = newOwnerId;
        room.UpdatedAt = DateTime.UtcNow;
        await _roomRepository.UpdateAsync(room);

        await UpdateUserTypeAccountAsync(newOwnerId);
        return true;
    }

    private async Task UpdateUserTypeAccountAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null && user.TypeAccount == "player")
        {
            user.TypeAccount = "host";
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var hostRole = await _roleRepository.GetByRoleNameAsync("host");
            if (hostRole != null)
            {
                await _userRoleRepository.DeleteByUserIdAsync(userId);
                var userRole = new UserRole(userId, hostRole.Id, DateTime.UtcNow, DateTime.UtcNow);
                await _userRoleRepository.AddAsync(userRole);
            }
        }
    }



    private async Task<string> GenerateUniqueRoomCodeAsync()
    {
        string roomCode;
        do
        {
            roomCode = GenerateRandomCode();
        } while (await _roomRepository.ExistsByCodeAsync(roomCode));
        
        return roomCode;
    }

    private string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private async Task SetupRoomSettingsAsync(int roomId, CreateRoomRequest request)
    {
        var settings = new List<RoomSetting>
        {
            new(roomId, "game_mode", request.GameMode),
            new(roomId, "topic_id", request.TopicId?.ToString() ?? "1"),
            new(roomId, "question_count", request.QuestionCount?.ToString() ?? "10"),
            new(roomId, "countdown_seconds", request.CountdownSeconds?.ToString() ?? "300")
        };

        foreach (var setting in settings)
        {
            await _roomSettingsRepository.AddSettingAsync(setting);
        }
        
        Console.WriteLine($"[CREATE_ROOM_SERVICE] Room {roomId} settings: GameMode={request.GameMode}, TopicId={request.TopicId ?? 1}, Questions={request.QuestionCount ?? 10}, Time={request.CountdownSeconds ?? 300}s");
    }


}