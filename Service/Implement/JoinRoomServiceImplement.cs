using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Mapper.Rooms;
using System.Collections.Concurrent;
namespace ConsoleApp1.Service.Implement;
public class JoinRoomServiceImplement : IJoinRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICreateRoomService _createRoomService;
    private readonly ISocketService _socketService;
    public readonly IBroadcastService _broadcastService;
    public JoinRoomServiceImplement(
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        ICreateRoomService createRoomService,
        ISocketService socketService,
        IBroadcastService broadcastService)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _createRoomService = createRoomService;
        _socketService = socketService;
        _broadcastService = broadcastService;
    }
    public async Task<RoomDTO?> JoinPublicRoomAsync(int roomId, int playerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return null;
        }
        if (room.IsPrivate || room.Status != "waiting")
        {
            return null;
        }
        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        if (playerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(roomId, "full");
            return null;
        }
        var existingPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(playerId, roomId);
        if (existingPlayer != null)
        {
            return RoomMapper.ToDTO(room);
        }
        var activeRoom = await _roomPlayerRepository.GetActiveRoomByUserIdAsync(playerId);
        if (activeRoom != null)
        {
            await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, activeRoom.Id);
            // Gửi sự kiện rời phòng qua WebSocket
            try 
            {
                await _socketService.LeaveRoomByUserIdAsync(playerId, activeRoom.RoomCode);
                // Đợi một chút để đảm bảo sự kiện rời phòng được xử lý
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
            }
        }
        var roomPlayer = new RoomPlayer 
        {
            RoomId = roomId,
            UserId = playerId,
            Score = 0,
            TimeTaken = TimeSpan.Zero,
            Status = "waiting",
            SocketId = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _roomPlayerRepository.AddAsync(roomPlayer);
        var newPlayerCount = playerCount + 1;
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        if (newPlayerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(roomId, "ready");
            room = await _roomRepository.GetByIdAsync(roomId);
        }
        var finalPlayerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            // Broadcast sự kiện player-joined cho các người chơi khác
            await _broadcastService.BroadcastPlayerJoinedAsync(room.RoomCode, playerId);
            // Broadcast cập nhật danh sách người chơi
            await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
        }
        catch (Exception ex)
        {
        }
        return RoomMapper.ToDTO(room);
    }
    public async Task<RoomDTO?> JoinPrivateRoomAsync(string roomCode, int playerId)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null)
        {
            return null;
        }
        if (room.Status != "waiting")
        {
            return null;
        }
        var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
        if (playerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(room.Id, "full");
            return null;
        }
        var existingPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(playerId, room.Id);
        if (existingPlayer != null)
        {
            return RoomMapper.ToDTO(room);
        }
        var activeRoom = await _roomPlayerRepository.GetActiveRoomByUserIdAsync(playerId);
        if (activeRoom != null)
        {
            await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, activeRoom.Id);
            // Gửi sự kiện rời phòng qua WebSocket
            try 
            {
                await _socketService.LeaveRoomByUserIdAsync(playerId, activeRoom.RoomCode);
                // Đợi một chút để đảm bảo sự kiện rời phòng được xử lý
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
            }
        }
        var roomPlayer = new RoomPlayer 
        {
            RoomId = room.Id,
            UserId = playerId,
            Score = 0,
            TimeTaken = TimeSpan.Zero,
            Status = "waiting",
            SocketId = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _roomPlayerRepository.AddAsync(roomPlayer);
        var newPlayerCount = playerCount + 1;
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        if (newPlayerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(room.Id, "ready");
            room = await _roomRepository.GetByIdAsync(room.Id);
        }
        var finalPlayerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            // Broadcast sự kiện player-joined cho các người chơi khác
            await _broadcastService.BroadcastPlayerJoinedAsync(room.RoomCode, playerId);
            // Broadcast cập nhật danh sách người chơi
            await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
        }
        catch (Exception ex)
        {
        }
        return RoomMapper.ToDTO(room);
    }
    public async Task<bool> LeaveRoomAsync(int roomId, int playerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        // Xóa player khỏi room_players TRƯỚC
        var deleteResult = await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, roomId);
        if (!deleteResult)
        {
            return false;
        }
        var playerCountAfter = await _roomRepository.GetPlayerCountAsync(roomId);
        // Cập nhật status nếu cần
        if (playerCountAfter < room.MaxPlayers && (room.Status == "full" || room.Status == "ready"))
        {
            await _roomRepository.UpdateStatusAsync(roomId, "waiting");
        }
        // Xử lý owner rời phòng SAU KHI đã xóa khỏi room_players
        if (room.OwnerId == playerId)
        {
            if (playerCountAfter == 0)
            {
                await _createRoomService.DeleteRoomAsync(roomId);
            }
            else
            {
                // Chuyển quyền owner cho người chơi đầu tiên còn lại
                var remainingPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
                if (remainingPlayers.Any())
                {
                    var nextOwner = remainingPlayers.OrderBy(p => p.CreatedAt).First();
                    await _createRoomService.TransferOwnershipAsync(roomId, nextOwner.UserId);
                }
            }
        }
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            var roomForBroadcast = await _roomRepository.GetByIdAsync(roomId);
            if (roomForBroadcast != null)
            {
                // Sử dụng phương thức LeaveRoomByUserIdAsync mới để xử lý rời phòng qua WebSocket
                await _socketService.LeaveRoomByUserIdAsync(playerId, roomForBroadcast.RoomCode);
            }
        }
        catch (Exception ex)
        {
        }
        return true;
    }
    public async Task<bool> LeaveRoomByCodeAsync(string roomCode, int playerId)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null) return false;
        return await LeaveRoomAsync(room.Id, playerId);
    }
    public async Task<RoomDTO?> GetRoomByCodeAsync(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            return null;
        }
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null)
        {
        }
        else
        {
        }
        return room != null ? RoomMapper.ToDTO(room) : null;
    }
    public async Task<IEnumerable<RoomSummaryDTO>> GetPublicRoomsAsync()
    {
        var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();
        var result = new List<RoomSummaryDTO>();
        foreach (var room in rooms)
        {
            var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
            var topicName = await _roomRepository.GetRoomTopicNameAsync(room.Id);
            var questionCount = await _roomRepository.GetRoomQuestionCountAsync(room.Id);
            var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(room.Id);
            result.Add(new RoomSummaryDTO(room.RoomCode, room.RoomName, room.IsPrivate, 
                playerCount, room.MaxPlayers, room.Status, topicName, questionCount, countdownTime));
        }
        return result;
    }
    public async Task<IEnumerable<RoomSummaryDTO>> GetAllRoomsAsync()
    {
        var rooms = await _roomRepository.GetAllRoomsWithDetailsAsync();
        if (rooms == null)
        {
            return new List<RoomSummaryDTO>();
        }
        var result = new List<RoomSummaryDTO>();
        foreach (var room in rooms)
        {
            var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
            var topicName = await _roomRepository.GetRoomTopicNameAsync(room.Id);
            var questionCount = await _roomRepository.GetRoomQuestionCountAsync(room.Id);
            var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(room.Id);
            result.Add(new RoomSummaryDTO(room.RoomCode, room.RoomName, room.IsPrivate, 
                playerCount, room.MaxPlayers, room.Status, topicName, questionCount, countdownTime));
        }
        return result;
    }
    // Dictionary để theo dõi thời gian gửi sự kiện cuối cùng cho mỗi phòng
    private readonly ConcurrentDictionary<string, DateTime> _lastGetPlayersTime = new();
    public async Task<IEnumerable<PlayerInRoomDTO>> GetPlayersInRoomAsync(int roomId)
    {
        var roomPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        var result = new List<PlayerInRoomDTO>();
        foreach (var rp in roomPlayers)
        {
            var user = await _userRepository.GetByIdAsync(rp.UserId);
            if (user != null)
            {
                result.Add(new PlayerInRoomDTO
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Score = rp.Score,
                    TimeTaken = rp.TimeTaken,
                    Status = rp.Status,
                    SocketId = rp.SocketId,
                    LastActivity = rp.LastActivity
                });
            }
        }
        // Tìm roomCode để gửi broadcast cập nhật
        try
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room != null)
            {
                // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 2 giây)
                string cacheKey = $"get_players_{room.RoomCode}";
                if (_lastGetPlayersTime.TryGetValue(cacheKey, out var lastTime) && 
                    (DateTime.UtcNow - lastTime).TotalMilliseconds < 2000)
                {
                }
                else
                {
                    // Cập nhật thời gian gửi mới nhất
                    _lastGetPlayersTime[cacheKey] = DateTime.UtcNow;
                    // Gửi broadcast cập nhật danh sách người chơi qua WebSocket
                    await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
                }
            }
        }
        catch (Exception ex)
        {
        }
        return result;
    }
    public async Task<RoomDetailsDTO?> GetRoomDetailsAsync(int roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return null;
        var owner = await _userRepository.GetByIdAsync(room.OwnerId);
        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        var topicName = await _roomRepository.GetRoomTopicNameAsync(roomId);
        var questionCount = await _roomRepository.GetRoomQuestionCountAsync(roomId);
        var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(roomId);
        var players = await GetPlayersInRoomAsync(roomId);
        return new RoomDetailsDTO
        {
            Id = room.Id,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            IsPrivate = room.IsPrivate,
            OwnerId = room.OwnerId,
            OwnerUsername = owner?.Username ?? "Unknown",
            Status = room.Status,
            MaxPlayers = room.MaxPlayers,
            CurrentPlayerCount = playerCount,
            TopicName = topicName ?? "Kiến thức chung",
            QuestionCount = questionCount,
            CountdownTime = countdownTime,
            GameMode = "battle", // Default, có thể lấy từ room_settings
            CreatedAt = room.CreatedAt,
            Players = players.ToList()
        };
    }
    public async Task<bool> StartGameAsync(int roomId, int userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null || room.OwnerId != userId || room.Status != "waiting") return false;
        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        if (playerCount < 2) return false; // Cần ít nhất 2 người chơi
        await _roomRepository.UpdateStatusAsync(roomId, "active");
        return true;
    }
    // Triển khai các phương thức broadcast từ interface IJoinRoomService
    public async Task BroadcastRoomPlayersUpdateAsync(string roomCode)
    {
        await _broadcastService.BroadcastRoomPlayersUpdateAsync(roomCode);
    }
    public async Task BroadcastPlayerJoinedAsync(string roomCode, int newPlayerId)
    {
        await _broadcastService.BroadcastPlayerJoinedAsync(roomCode, newPlayerId);
    }
}
