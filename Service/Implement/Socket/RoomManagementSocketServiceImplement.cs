using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.RoomManagement;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

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

    /// <summary>
    /// Xử lý khi người chơi tham gia phòng
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng muốn tham gia</param>
    /// <param name="username">Tên người chơi</param>
    /// <param name="userId">ID người chơi trong database</param>
    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId)
    {
        Console.WriteLine($"[ROOM] JoinRoomAsync được gọi - socketId: {socketId}, mã phòng: {roomCode}, tên người dùng: {username}, userId: {userId}");
        
        try
        {
            // Thêm player vào room
            var (success, message, player) = _roomManager.AddPlayerToRoom(roomCode, socketId, username, userId);
            
            if (!success)
            {
                Console.WriteLine($"[ROOM] Thất bại thêm người chơi {username} vào phòng {roomCode}: {message}");
                return;
            }

            if (player == null)
            {
                Console.WriteLine($"[ROOM] Đối tượng người chơi là null cho {username}");
                return;
            }

            // Gửi thông báo welcome cho player vừa join
            await _eventBroadcaster.SendWelcomeMessageAsync(socketId, roomCode, player.IsHost, message);
            
            // Broadcast sự kiện player-joined cho các user khác trong phòng (không gửi cho chính user vừa join)
            await BroadcastPlayerJoinedEventAsync(roomCode, userId, username);
            
            // Cập nhật danh sách player cho tất cả client trong phòng
            await UpdateRoomPlayersAsync(roomCode);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ROOM] Lỗi trong JoinRoomAsync: {ex.Message}");
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
            var (success, message, removedPlayer, newHost) = _roomManager.RemovePlayerFromRoom(socketId, roomCode);
            
            if (!success)
            {
                Console.WriteLine($"[ROOM] Thất bại xóa người chơi khỏi phòng {roomCode}: {message}");
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
                await UpdateRoomPlayersAsync(roomCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ROOM] Lỗi trong LeaveRoomAsync: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật danh sách người chơi trong phòng cho tất cả client
    /// </summary>
    /// <param name="roomCode">Mã phòng cần cập nhật</param>
    public async Task UpdateRoomPlayersAsync(string roomCode)
    {
        try
        {
            var room = _roomManager.GetRoom(roomCode);
            if (room == null || room.Players.Count == 0)
            {
                Console.WriteLine($"[ROOM] Không tìm thấy người chơi nào cho phòng {roomCode}");
                return;
            }

            await _eventBroadcaster.BroadcastRoomPlayersUpdateAsync(roomCode, room.Players);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ROOM] Lỗi cập nhật người chơi phòng cho {roomCode}: {ex.Message}");
        }
    }

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
            var playerJoinedData = new
            {
                UserId = userId,
                Username = username,
                Score = 0,
                TimeTaken = "00:00:00"
            };

            // Broadcast chỉ tới những người đã có trong phòng trước đó (loại trừ người vừa join)
            await _eventBroadcaster.BroadcastToOthersAsync(roomCode, userId, "player-joined", playerJoinedData);
            
            Console.WriteLine($"[ROOM] Broadcasted player-joined event for {username} (ID: {userId}) to others in room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ROOM] Lỗi broadcast player-joined event: {ex.Message}");
        }
    }

}