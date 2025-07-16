using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
    // Dictionary lưu trữ tất cả các phòng game hiện tại
    // Key: roomCode, Value: GameRoom object
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary ánh xạ socketId với roomCode
    // Key: socketId, Value: roomCode mà socket đang tham gia
    private readonly ConcurrentDictionary<string, string> _socketToRoom = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (shared với ConnectionService)
    // Key: socketId, Value: WebSocket connection
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    /// <summary>
    /// Xử lý khi người chơi tham gia phòng
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng muốn tham gia</param>
    /// <param name="username">Tên người chơi</param>
    /// <param name="userId">ID người chơi trong database</param>
    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId)
    {
        Console.WriteLine($"[ROOM] JoinRoomAsync called - socketId: {socketId}, roomCode: {roomCode}, username: {username}, userId: {userId}");
        
        // Lưu mapping socketId -> roomCode
        _socketToRoom[socketId] = roomCode;
        
        // Tạo phòng mới nếu chưa tồn tại
        if (!_gameRooms.ContainsKey(roomCode))
        {
            _gameRooms[roomCode] = new GameRoom { RoomCode = roomCode };
            Console.WriteLine($"[ROOM] Created new game room: {roomCode}");
        }
        
        var gameRoom = _gameRooms[roomCode];
        
        // Kiểm tra xem player đã tồn tại chưa (tránh duplicate)
        var existingPlayer = gameRoom.Players.FirstOrDefault(p => p.UserId == userId);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[ROOM] Player {username} (ID: {userId}) already exists in room {roomCode}, updating socket");
            existingPlayer.SocketId = socketId; // Cập nhật socketId mới
            await UpdateRoomPlayersAsync(roomCode);
            return;
        }
        
        // Kiểm tra xem có phải người đầu tiên không (sẽ là host)
        var isFirstPlayer = gameRoom.Players.Count == 0;
        
        // Tạo player mới
        var player = new GamePlayer
        {
            Username = username,
            UserId = userId,
            SocketId = socketId,
            IsHost = isFirstPlayer, // Người đầu tiên là host
            JoinTime = DateTime.UtcNow
        };
        
        // Thêm player vào phòng
        gameRoom.Players.Add(player);
        
        // Log thông tin join
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] 🔌 WEBSOCKET JOIN - Room {roomCode}: {username} (ID: {userId}) joined as {(player.IsHost ? "HOST" : "player")}");
        Console.WriteLine($"[{timestamp}] 📊 WEBSOCKET ROOM STATUS - Room {roomCode}: {gameRoom.Players.Count} players connected | Host: {gameRoom.Players.FirstOrDefault(p => p.IsHost)?.Username}");
        
        // Gửi thông báo welcome cho player vừa join
        await SendToPlayerAsync(socketId, "room-joined", new {
            roomCode = roomCode,
            isHost = player.IsHost,
            message = $"Chào mừng {username} đến phòng {roomCode}!"
        });
        
        // Cập nhật danh sách player cho tất cả client trong phòng
        await UpdateRoomPlayersAsync(roomCode);
    }

    /// <summary>
    /// Xử lý khi người chơi rời phòng
    /// </summary>
    /// <param name="socketId">ID của WebSocket connection</param>
    /// <param name="roomCode">Mã phòng đang tham gia</param>
    public async Task LeaveRoomAsync(string socketId, string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.SocketId == socketId);
        
        if (player != null)
        {
            // Xóa player khỏi phòng
            gameRoom.Players.Remove(player);
            _socketToRoom.TryRemove(socketId, out _);
            
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            Console.WriteLine($"[{timestamp}] 🚪 WEBSOCKET LEAVE - Room {roomCode}: {player.Username} (ID: {player.UserId}) left");
            
            // Nếu là host rời phòng và còn người khác
            if (player.IsHost && gameRoom.Players.Count > 0)
            {
                // Chuyển host cho người join sớm nhất tiếp theo
                var nextHost = gameRoom.Players.OrderBy(p => p.JoinTime ?? DateTime.MaxValue).First();
                nextHost.IsHost = true;
                
                Console.WriteLine($"[{timestamp}] 👑 HOST TRANSFER - Room {roomCode}: {nextHost.Username} is now the host");
                
                // Thông báo host mới cho tất cả client
                await BroadcastToRoomAsync(roomCode, "host-changed", new {
                    newHost = nextHost.Username,
                    newHostId = nextHost.UserId,
                    message = $"{nextHost.Username} đã trở thành host mới"
                });
            }
            
            // Nếu không còn ai trong phòng thì xóa phòng
            if (gameRoom.Players.Count == 0)
            {
                _gameRooms.TryRemove(roomCode, out _);
                Console.WriteLine($"[{timestamp}] 🗑️ ROOM DELETED - Room {roomCode}: No players remaining");
                
                // Dọn dẹp game session nếu có game đang chạy
                // Lưu ý: Trong thực tế cần gọi GameFlowSocketService.CleanupGameSessionAsync
                Console.WriteLine($"[ROOM] Game session cleanup needed for empty room {roomCode}");
            }
            else
            {
                // Cập nhật danh sách player cho những người còn lại
                await UpdateRoomPlayersAsync(roomCode);
            }
        }
    }

    /// <summary>
    /// Cập nhật danh sách người chơi trong phòng cho tất cả client
    /// </summary>
    /// <param name="roomCode">Mã phòng cần cập nhật</param>
    public async Task UpdateRoomPlayersAsync(string roomCode)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        // Tạo danh sách player để gửi cho client
        var playerList = gameRoom.Players.Select(p => new {
            username = p.Username,
            userId = p.UserId,
            isHost = p.IsHost,
            joinTime = p.JoinTime,
            isOnline = _connections.ContainsKey(p.SocketId ?? "") && 
                      _connections.TryGetValue(p.SocketId ?? "", out var ws) && 
                      ws.State == System.Net.WebSockets.WebSocketState.Open // Kiểm tra trạng thái kết nối thực tế
        }).ToList();
        
        // Broadcast danh sách player mới cho tất cả client trong phòng
        await BroadcastToRoomAsync(roomCode, "room-players-updated", new {
            roomCode = roomCode,
            players = playerList,
            totalPlayers = playerList.Count,
            host = playerList.FirstOrDefault(p => p.isHost)?.username
        });
        
        Console.WriteLine($"[ROOM] Updated player list for room {roomCode}: {playerList.Count} players");
    }
    
    /// <summary>
    /// Gửi message đến tất cả client trong một phòng cụ thể
    /// </summary>
    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var message = JsonSerializer.Serialize(new {
            eventName = eventName,
            data = data
        });
        var buffer = Encoding.UTF8.GetBytes(message);
        
        // Gửi đến tất cả player trong phòng
        var sendTasks = gameRoom.Players
            .Where(p => !string.IsNullOrEmpty(p.SocketId))
            .Select(async player =>
            {
                if (_connections.TryGetValue(player.SocketId!, out var socket) && 
                    socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROOM] Failed to send message to {player.Username}: {ex.Message}");
                    }
                }
            });
        
        await Task.WhenAll(sendTasks);
    }
    
    /// <summary>
    /// Gửi message đến một client cụ thể
    /// </summary>
    private async Task SendToPlayerAsync(string socketId, string eventName, object data)
    {
        if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
        {
            try
            {
                var message = JsonSerializer.Serialize(new {
                    eventName = eventName,
                    data = data
                });
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ROOM] Failed to send message to socket {socketId}: {ex.Message}");
            }
        }
    }
}