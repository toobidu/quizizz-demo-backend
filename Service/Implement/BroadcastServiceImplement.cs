using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
namespace ConsoleApp1.Service.Implement;
public class BroadcastServiceImplement : IBroadcastService
{
    private readonly ISocketService _socketService;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private IJoinRoomService? _joinRoomService;
    public BroadcastServiceImplement(
        ISocketService socketService,
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IJoinRoomService? joinRoomService)
    {
        _socketService = socketService;
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _joinRoomService = joinRoomService;
    }
    public void SetJoinRoomService(IJoinRoomService joinRoomService)
    {
        _joinRoomService = joinRoomService;
    }
    // Dictionary để theo dõi thời gian gửi sự kiện cuối cùng cho mỗi phòng
    private readonly ConcurrentDictionary<string, DateTime> _lastBroadcastTimes = new();
    public async Task BroadcastRoomPlayersUpdateAsync(string roomCode)
    {
        try
        {
            // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 1 giây)
            string cacheKey = $"broadcast_{roomCode}";
            if (_lastBroadcastTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 1000)
            {
                return;
            }
            // Cập nhật thời gian gửi mới nhất
            _lastBroadcastTimes[cacheKey] = DateTime.UtcNow;
            var room = await _roomRepository.GetByCodeAsync(roomCode);
            if (room == null) 
            {
                return;
            }
            // Lấy danh sách players trong phòng
            var roomPlayers = await _roomPlayerRepository.GetByRoomIdAsync(room.Id);
            var playerDetails = new List<object>();
            foreach (var rp in roomPlayers)
            {
                var user = await _userRepository.GetByIdAsync(rp.UserId);
                if (user != null)
                {
                    playerDetails.Add(new
                    {
                        userId = user.Id,
                        username = user.Username,
                        isHost = room.OwnerId == user.Id,
                        joinTime = rp.CreatedAt,
                        score = rp.Score,
                        timeTaken = "00:00:00" // Thêm trường timeTaken để đảm bảo định dạng nhất quán
                    });
                }
            }
            var message = new
            {
                Type = "ROOM_PLAYERS_UPDATED",
                RoomCode = roomCode,
                Data = new
                {
                    roomCode = roomCode,
                    players = playerDetails,
                    totalPlayers = playerDetails.Count,
                    maxPlayers = room.MaxPlayers,
                    status = room.Status,
                    host = playerDetails.FirstOrDefault(p => (bool)p.GetType().GetProperty("isHost")!.GetValue(p)!)
                },
                Timestamp = DateTime.UtcNow
            };
            // Log chi tiết về danh sách người chơi được gửi
            // Chỉ gửi một lần duy nhất qua WebSocket
            try {
                // Gửi trực tiếp sự kiện với dữ liệu đầy đủ
                await _socketService.BroadcastToAllConnectionsAsync(roomCode, "room-players-updated", message.Data);
            }
            catch (Exception ex) {
            }
        }
        catch (Exception ex)
        {
        }
    }
    public async Task BroadcastPlayerJoinedAsync(string roomCode, int newPlayerId)
    {
        try
        {
            var room = await _roomRepository.GetByCodeAsync(roomCode);
            if (room == null) return;
            var user = await _userRepository.GetByIdAsync(newPlayerId);
            if (user == null) return;
            var roomPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(newPlayerId, room.Id);
            if (roomPlayer == null) return;
            // Broadcast sự kiện PlayerJoined trực tiếp qua WebSocket
            await _socketService.BroadcastPlayerJoinedEventAsync(roomCode, user.Id, user.Username);
            // Cập nhật danh sách người chơi cho tất cả client trong phòng
            await BroadcastRoomPlayersUpdateAsync(roomCode);
        }
        catch (Exception ex)
        {
        }
    }
    public async Task BroadcastRoomCreatedAsync(RoomDTO room)
    {
        try
        {
            var message = new
            {
                Type = "ROOM_CREATED",
                RoomCode = room.Code,
                Data = room,
                Timestamp = DateTime.UtcNow
            };
            // Broadcast danh sách phòng mới cho lobby
            await BroadcastRoomsListUpdateAsync();
        }
        catch (Exception ex)
        {
        }
    }
    public async Task BroadcastRoomDeletedAsync(string roomCode)
    {
        try
        {
            var message = new
            {
                Type = "ROOM_DELETED",
                RoomCode = roomCode,
                Data = new { roomCode },
                Timestamp = DateTime.UtcNow
            };
            // Broadcast danh sách phòng mới cho lobby
            await BroadcastRoomsListUpdateAsync();
        }
        catch (Exception ex)
        {
        }
    }
    public async Task BroadcastRoomsListUpdateAsync()
    {
        try
        {
            if (_joinRoomService == null)
            {
                return;
            }
            // Lấy danh sách tất cả phòng
            var rooms = await _joinRoomService.GetAllRoomsAsync();
            var message = new
            {
                Type = "ROOMS_LIST_UPDATED",
                RoomCode = (string?)null,
                Data = new
                {
                    rooms = rooms,
                    totalRooms = rooms.Count()
                },
                Timestamp = DateTime.UtcNow
            };
            // TODO: Broadcast cho tất cả client đang ở lobby
            // await _socketService.BroadcastToAllAsync(message);
        }
        catch (Exception ex)
        {
        }
    }
    public async Task BroadcastSyncRoomJoinAsync(string roomCode, int userId, string username)
    {
        try
        {
            // Gửi sự kiện đồng bộ để frontend biết cần gửi WebSocket joinRoom
            await _socketService.BroadcastToAllConnectionsAsync(roomCode, "sync-room-join", new {
                roomCode = roomCode,
                userId = userId,
                username = username,
                action = "join"
            });
        }
        catch (Exception ex)
        {
        }
    }
}
