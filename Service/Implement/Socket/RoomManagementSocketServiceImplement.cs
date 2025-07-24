using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.RoomManagement;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using ConsoleApp1.Config;
namespace ConsoleApp1.Service.Implement.Socket;
/// <summary>
/// Service quản lý phòng chơi qua WebSocket - Chịu trách nhiệm:
/// 1. Xử lý việc tham gia phòng (join room)
/// 2. Xử lý việc rời phòng (leave room)  
/// 3. Cập nhật danh sách người chơi trong phòng
/// 4. Broadcast thông tin phòng đến tất cả client
/// 5. Quản lý host và chuyển quyền host
/// </summary>
public class RoomManagementSocketServiceImplement : IRoomManagementSocketService
{
    // Dictionary lưu trữ tất cả các phòng game hiện tại (shared)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    // Dictionary ánh xạ socketId với roomCode (shared)
    private readonly ConcurrentDictionary<string, string> _socketToRoom;
    // Dictionary lưu trữ các kết nối WebSocket (shared)
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    // Components
    private readonly RoomManager _roomManager;
    private readonly RoomEventBroadcaster _eventBroadcaster;
    /// <summary>
    /// Constructor nhận shared dictionaries
    /// </summary>
    public RoomManagementSocketServiceImplement(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, string> socketToRoom,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _socketToRoom = socketToRoom;
        _connections = connections;
        _roomManager = new RoomManager(_gameRooms, _socketToRoom, _connections);
        _eventBroadcaster = new RoomEventBroadcaster(_gameRooms, _connections);
    }
    /// <summary>
    /// Constructor mặc định (backward compatibility)
    /// </summary>
    public RoomManagementSocketServiceImplement()
    {
        _gameRooms = new ConcurrentDictionary<string, GameRoom>();
        _socketToRoom = new ConcurrentDictionary<string, string>();
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _roomManager = new RoomManager(_gameRooms, _socketToRoom, _connections);
        _eventBroadcaster = new RoomEventBroadcaster(_gameRooms, _connections);
    }
    // Dictionary để theo dõi thời gian join phòng cuối cùng của mỗi người chơi
    private readonly ConcurrentDictionary<string, DateTime> _lastJoinTimes = new();
    /// <summary>
    /// Xử lý khi người chơi tham gia phòng
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng muốn tham gia</param>
    /// <param name="username">Tên người chơi</param>
    /// <param name="userId">ID người chơi trong database</param>
    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId)
    {
        try
        {
            // Kiểm tra xem người chơi đã join gần đây chưa (trong vòng 2 giây)
            string cacheKey = $"join_{roomCode}_{userId}";
            if (_lastJoinTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 2000)
            {
                // Vẫn gửi thông tin phòng hiện tại cho client
                var existingRoom = _roomManager.GetRoom(roomCode);
                if (existingRoom != null)
                {
                    var existingRoomData = new
                    {
                        roomCode = roomCode,
                        players = existingRoom.Players.Select(p => new {
                            userId = p.UserId,
                            username = p.Username,
                            score = p.Score,
                            isHost = p.IsHost,
                            status = p.Status,
                            timeTaken = "00:00:00"
                        }).ToList(),
                        totalPlayers = existingRoom.Players.Count,
                        maxPlayers = 10, // Giá trị mặc định vì GameRoom không có thuộc tính MaxPlayers
                        status = "waiting", // Giá trị mặc định vì GameRoom không có thuộc tính Status
                        host = existingRoom.Players.FirstOrDefault(p => p.IsHost)?.Username
                    };
                    await _eventBroadcaster.SendToPlayerAsync(socketId, RoomManagementConstants.Events.RoomPlayersUpdated, existingRoomData);
                }
                return;
            }
            // Cập nhật thời gian join mới nhất
            _lastJoinTimes[cacheKey] = DateTime.UtcNow;
            // Thêm player vào room
            var (success, message, player) = _roomManager.AddPlayerToRoom(roomCode, socketId, username, userId);
            if (!success)
            {
                return;
            }
            if (player == null)
            {
                return;
            }
            // Lấy thông tin phòng để gửi thông tin về tất cả người chơi
            var room = _roomManager.GetRoom(roomCode);
            if (room == null)
            {
                return;
            }
            // Gửi thông báo welcome cho player vừa join
            await _eventBroadcaster.SendWelcomeMessageAsync(socketId, roomCode, player.IsHost, message);
            // Gửi thông tin về tất cả người chơi hiện có trong phòng cho người mới tham gia
            foreach (var existingPlayer in room.Players.Where(p => p.UserId != userId))
            {
                var playerData = new
                {
                    userId = existingPlayer.UserId,
                    username = existingPlayer.Username,
                    score = existingPlayer.Score,
                    isHost = existingPlayer.IsHost,
                    timeTaken = "00:00:00"
                };
                await _eventBroadcaster.SendToPlayerAsync(socketId, RoomManagementConstants.Events.PlayerJoined, playerData);
            }
            // Gửi sự kiện room-players-updated cho người chơi mới
            var roomPlayersData = new
            {
                roomCode = roomCode,
                players = room.Players.Select(p => new {
                    userId = p.UserId,
                    username = p.Username,
                    score = p.Score,
                    isHost = p.IsHost,
                    status = p.Status,
                    timeTaken = "00:00:00"
                }).ToList(),
                totalPlayers = room.Players.Count,
                maxPlayers = 10, // Giá trị mặc định vì GameRoom không có thuộc tính MaxPlayers
                status = "waiting", // Giá trị mặc định vì GameRoom không có thuộc tính Status
                host = room.Players.FirstOrDefault(p => p.IsHost)?.Username
            };
            await _eventBroadcaster.SendToPlayerAsync(socketId, RoomManagementConstants.Events.RoomPlayersUpdated, roomPlayersData);
            // Broadcast sự kiện player-joined cho các user khác trong phòng (không gửi cho chính user vừa join)
            await BroadcastPlayerJoinedEventAsync(roomCode, userId, username);
            // Gửi sự kiện room-players-updated đến tất cả người chơi trong phòng
            // Đảm bảo sự kiện này được gửi sau sự kiện player-joined
            await UpdateRoomPlayersAsync(roomCode);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý khi người chơi rời phòng
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng đang tham gia</param>
    public async Task LeaveRoomAsync(string socketId, string roomCode)
    {
        try
        {
            // Lưu thông tin phòng và người chơi trước khi xóa để sử dụng sau
            var room = _roomManager.GetRoom(roomCode);
            var playerToRemove = room?.Players.FirstOrDefault(p => p.SocketId == socketId);
            if (room == null || playerToRemove == null)
            {
                return;
            }
            // Log thông tin người chơi sẽ bị xóa
            // Lưu userId và username để sử dụng sau khi xóa
            int userId = playerToRemove.UserId;
            string username = playerToRemove.Username;
            // Xóa người chơi khỏi phòng
            var (success, message, removedPlayer, newHost) = _roomManager.RemovePlayerFromRoom(socketId, roomCode);
            if (!success)
            {
                return;
            }
            // Thông báo host mới nếu có
            if (newHost != null)
            {
                await _eventBroadcaster.BroadcastHostChangeAsync(roomCode, newHost);
            }
            // Cập nhật danh sách player nếu room còn tồn tại
            if (_roomManager.RoomExists(roomCode))
            {
                var updatedRoom = _roomManager.GetRoom(roomCode);
                if (updatedRoom != null)
                {
                    // Gửi sự kiện player-left trước
                    var playerLeftData = new
                    {
                        userId = userId,
                        username = username
                    };
                    await _eventBroadcaster.BroadcastToRoomAsync(roomCode, "player-left", playerLeftData);
                    // Đợi một chút để đảm bảo client xử lý sự kiện player-left trước
                    await Task.Delay(200);
                    // Sau đó gửi cập nhật danh sách người chơi
                    await _eventBroadcaster.BroadcastRoomPlayersUpdateAsync(roomCode, updatedRoom.Players);
                }
            }
        }
        catch (Exception ex)
        {
        }
    }
    // Dictionary để theo dõi thời gian gửi sự kiện cuối cùng cho mỗi phòng
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();
    /// <summary>
    /// Cập nhật danh sách người chơi trong phòng cho tất cả client
    /// </summary>
    /// <param name="roomCode">Mã phòng cần cập nhật</param>
    public async Task UpdateRoomPlayersAsync(string roomCode)
    {
        try
        {
            // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 1 giây)
            string cacheKey = $"update_{roomCode}";
            if (_lastUpdateTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 1000)
            {
                return;
            }
            // Cập nhật thời gian gửi mới nhất
            _lastUpdateTimes[cacheKey] = DateTime.UtcNow;
            var room = _roomManager.GetRoom(roomCode);
            if (room == null)
            {
                return;
            }
            if (room.Players.Count == 0)
            {
                // Vẫn gửi cập nhật với danh sách trống để frontend cập nhật
                await _eventBroadcaster.BroadcastRoomPlayersUpdateAsync(roomCode, new List<GamePlayer>());
                return;
            }
            // Chỉ gửi một lần duy nhất
            await _eventBroadcaster.BroadcastRoomPlayersUpdateAsync(roomCode, room.Players);
        }
        catch (Exception ex)
        {
        }
    }
    // Dictionary để theo dõi thời gian gửi sự kiện player-joined cuối cùng cho mỗi người chơi
    private readonly ConcurrentDictionary<string, DateTime> _lastPlayerJoinedTimes = new();
    /// <summary>
    /// Broadcast sự kiện player-joined chỉ tới những người đã có trong phòng trước đó
    /// KHÔNG gửi cho người vừa join để tránh duplicate event
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="userId">ID người chơi mới</param>
    /// <param name="username">Tên người chơi mới</param>
    public async Task BroadcastPlayerJoinedEventAsync(string roomCode, int userId, string username)
    {
        try
        {
            // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 2 giây)
            string cacheKey = $"player_joined_{roomCode}_{userId}";
            if (_lastPlayerJoinedTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 2000)
            {
                return;
            }
            // Cập nhật thời gian gửi mới nhất
            _lastPlayerJoinedTimes[cacheKey] = DateTime.UtcNow;
            var playerJoinedData = new
            {
                userId = userId,  // Chuyển sang camelCase để phù hợp với frontend
                username = username,
                score = 0,
                timeTaken = "00:00:00"
            };
            // Broadcast chỉ tới những người đã có trong phòng trước đó (loại trừ người vừa join)
            await _eventBroadcaster.BroadcastToOthersAsync(roomCode, userId, RoomManagementConstants.Events.PlayerJoined, playerJoinedData);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý khi người chơi rời phòng theo userId
    /// </summary>
    /// <param name="userId">ID của người chơi</param>
    /// <param name="roomCode">Mã phòng đang tham gia</param>
    public async Task LeaveRoomByUserIdAsync(int userId, string roomCode)
    {
        try
        {
            // Lưu thông tin phòng và người chơi trước khi xóa để sử dụng sau
            var room = _roomManager.GetRoom(roomCode);
            var playerToRemove = room?.Players.FirstOrDefault(p => p.UserId == userId);
            if (room == null || playerToRemove == null)
            {
                return;
            }
            // Log thông tin người chơi sẽ bị xóa
            // Lưu thông tin người chơi để sử dụng sau khi xóa
            string username = playerToRemove.Username;
            string? socketId = playerToRemove.SocketId;
            // Xóa người chơi khỏi phòng
            var (success, message, removedPlayer, newHost) = _roomManager.RemovePlayerFromRoomByUserId(userId, roomCode);
            if (!success)
            {
                return;
            }
            // Thông báo host mới nếu có
            if (newHost != null)
            {
                await _eventBroadcaster.BroadcastHostChangeAsync(roomCode, newHost);
            }
            // Cập nhật danh sách player nếu room còn tồn tại
            if (_roomManager.RoomExists(roomCode))
            {
                var updatedRoom = _roomManager.GetRoom(roomCode);
                if (updatedRoom != null)
                {
                    // Gửi sự kiện player-left trước
                    var playerLeftData = new
                    {
                        userId = userId,
                        username = username
                    };
                    await _eventBroadcaster.BroadcastToRoomAsync(roomCode, "player-left", playerLeftData);
                    // Đợi một chút để đảm bảo client xử lý sự kiện player-left trước
                    await Task.Delay(200);
                    // Sau đó gửi cập nhật danh sách người chơi
                    await _eventBroadcaster.BroadcastRoomPlayersUpdateAsync(roomCode, updatedRoom.Players);
                }
            }
            // Xóa socket connection nếu có
            if (!string.IsNullOrEmpty(socketId) && _connections.ContainsKey(socketId))
            {
                _connections.TryRemove(socketId, out _);
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Broadcast sự kiện player-left tới tất cả người chơi trong phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="userId">ID người chơi rời phòng</param>
    /// <param name="username">Tên người chơi rời phòng</param>
    public async Task BroadcastPlayerLeftEventAsync(string roomCode, int userId, string username)
    {
        try
        {
            var playerLeftData = new
            {
                userId = userId,
                username = username
            };
            // Broadcast tới tất cả người chơi trong phòng
            await _eventBroadcaster.BroadcastToRoomAsync(roomCode, "player-left", playerLeftData);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Broadcast message đến tất cả WebSocket connections hiện tại
    /// Dùng để gửi thông báo đồng bộ giữa HTTP và WebSocket
    /// </summary>
    public async Task BroadcastToAllConnectionsAsync(string roomCode, string eventName, object data)
    {
        try
        {
            await _eventBroadcaster.BroadcastToAllConnectionsAsync(roomCode, eventName, data);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý yêu cầu cập nhật danh sách người chơi từ client
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng cần cập nhật</param>
    public async Task RequestPlayersUpdateAsync(string socketId, string roomCode)
    {
        try
        {
            var room = _roomManager.GetRoom(roomCode);
            if (room == null)
            {
                return;
            }
            // Gửi cập nhật danh sách người chơi chỉ cho client yêu cầu
            var roomPlayersData = new
            {
                roomCode = roomCode,
                players = room.Players.Select(p => new {
                    userId = p.UserId,
                    username = p.Username,
                    score = p.Score,
                    isHost = p.IsHost,
                    status = p.Status,
                    timeTaken = "00:00:00"
                }).ToList(),
                totalPlayers = room.Players.Count,
                maxPlayers = 10,
                status = "waiting",
                host = room.Players.FirstOrDefault(p => p.IsHost)?.Username
            };
            await _eventBroadcaster.SendToPlayerAsync(socketId, RoomManagementConstants.Events.RoomPlayersUpdated, roomPlayersData);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Lấy thông tin phòng theo mã phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng cần lấy thông tin</param>
    /// <returns>Thông tin phòng hoặc null nếu không tìm thấy</returns>
    public Task<GameRoom?> GetRoomAsync(string roomCode)
    {
        var room = _roomManager.GetRoom(roomCode);
        return Task.FromResult(room);
    }
}
