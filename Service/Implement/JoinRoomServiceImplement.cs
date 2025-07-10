using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Mapper.Rooms;

namespace ConsoleApp1.Service.Implement;

public class JoinRoomServiceImplement : IJoinRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICreateRoomService _createRoomService;

    public JoinRoomServiceImplement(
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        ICreateRoomService createRoomService)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _createRoomService = createRoomService;
    }

    public async Task<RoomDTO?> JoinPublicRoomAsync(int roomId, int playerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null || room.IsPrivate || room.Status != "waiting") return null;

        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        if (playerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(roomId, "full");
            return null;
        }

        var roomPlayer = new RoomPlayer(roomId, playerId, 0, TimeSpan.Zero, DateTime.UtcNow, DateTime.UtcNow);
        await _roomPlayerRepository.AddAsync(roomPlayer);

        var newPlayerCount = playerCount + 1;
        if (newPlayerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(roomId, "full");
        }

        return RoomMapper.ToDTO(room);
    }

    public async Task<RoomDTO?> JoinPrivateRoomAsync(string roomCode, int playerId)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null || !room.IsPrivate || room.Status != "waiting") return null;

        var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
        if (playerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(room.Id, "full");
            return null;
        }

        var roomPlayer = new RoomPlayer(room.Id, playerId, 0, TimeSpan.Zero, DateTime.UtcNow, DateTime.UtcNow);
        await _roomPlayerRepository.AddAsync(roomPlayer);

        var newPlayerCount = playerCount + 1;
        if (newPlayerCount >= room.MaxPlayers)
        {
            await _roomRepository.UpdateStatusAsync(room.Id, "full");
        }

        return RoomMapper.ToDTO(room);
    }

    public async Task<bool> LeaveRoomAsync(int roomId, int playerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null || (room.Status != "waiting" && room.Status != "full")) return false;

        await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, roomId);

        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        if (playerCount < room.MaxPlayers && room.Status == "full")
        {
            await _roomRepository.UpdateStatusAsync(roomId, "waiting");
        }

        if (room.OwnerId == playerId)
        {
            await TransferOwnershipOrDeleteRoomAsync(roomId);
        }

        return true;
    }

    public async Task<RoomDTO?> GetRoomByCodeAsync(string roomCode)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        return room != null ? RoomMapper.ToDTO(room) : null;
    }

    public async Task<IEnumerable<RoomSummaryDTO>> GetPublicRoomsAsync()
    {
        var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();
        
        var result = new List<RoomSummaryDTO>();
        foreach (var room in rooms)
        {
            var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
            result.Add(RoomMapper.ToSummaryDTO(room, playerCount));
        }
        
        return result;
    }

    public async Task<IEnumerable<PlayerInRoomDTO>> GetPlayersInRoomAsync(int roomId)
    {
        var roomPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        var result = new List<PlayerInRoomDTO>();

        foreach (var rp in roomPlayers)
        {
            var user = await _userRepository.GetByIdAsync(rp.UserId);
            if (user != null)
            {
                result.Add(new PlayerInRoomDTO(user.Id, user.Username, rp.Score, rp.TimeTaken));
            }
        }

        return result;
    }

    private async Task TransferOwnershipOrDeleteRoomAsync(int roomId)
    {
        var remainingPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        
        if (!remainingPlayers.Any())
        {
            await _createRoomService.DeleteRoomAsync(roomId);
        }
        else
        {
            var nextOwner = remainingPlayers.OrderBy(p => p.CreatedAt).First();
            await _createRoomService.TransferOwnershipAsync(roomId, nextOwner.UserId);
        }
    }
}