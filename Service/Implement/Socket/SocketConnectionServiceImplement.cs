using ConsoleApp1.Service.Interface.Socket;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
namespace ConsoleApp1.Service.Implement.Socket;
/// <summary>
/// Service quản lý kết nối WebSocket - Chịu trách nhiệm:
/// 1. Khởi động/dừng WebSocket server
/// 2. Xử lý các kết nối WebSocket mới
/// 3. Quản lý danh sách các kết nối đang hoạt động
/// 4. Xử lý ping/pong để giữ kết nối sống
/// </summary>
public class SocketConnectionServiceImplement : ISocketConnectionService
{
    // Dictionary lưu trữ tất cả các kết nối WebSocket hiện tại (shared)
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    // Dictionary ánh xạ socketId với roomCode (shared)
    private readonly ConcurrentDictionary<string, string> _socketToRoom;
    // HttpListener để lắng nghe các WebSocket request
    private HttpListener? _listener;
    /// <summary>
    /// Constructor nhận shared dictionaries
    /// </summary>
    public SocketConnectionServiceImplement(
        ConcurrentDictionary<string, WebSocket> connections,
        ConcurrentDictionary<string, string> socketToRoom)
    {
        _connections = connections;
        _socketToRoom = socketToRoom;
    }
    /// <summary>
    /// Constructor mặc định (backward compatibility)
    /// </summary>
    public SocketConnectionServiceImplement()
    {
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _socketToRoom = new ConcurrentDictionary<string, string>();
    }
    /// <summary>
    /// Khởi động WebSocket server trên port được chỉ định
    /// </summary>
    /// <param name="port">Port để lắng nghe WebSocket connections</param>
    public async Task StartAsync(int port)
    {
        // Khởi tạo HttpListener
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        // Chạy vòng lặp lắng nghe các connection mới trong background
        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                try
                {
                    // Chờ connection mới
                    var context = await _listener.GetContextAsync();
                    // Xử lý mỗi connection trong task riêng biệt
                    _ = Task.Run(() => HandleWebSocketRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // Server đã dừng
                    break;
                }
                catch (Exception ex)
                {
                }
            }
        });
        // Đợi một chút để đảm bảo server đã start
        await Task.Delay(100);
    }
    /// <summary>
    /// Dừng WebSocket server và đóng tất cả kết nối
    /// </summary>
    public async Task StopAsync()
    {
        // Đóng tất cả các kết nối WebSocket
        foreach (var connection in _connections.Values)
        {
            if (connection.State == WebSocketState.Open)
            {
                await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None);
            }
        }
        _connections.Clear();
        // Dừng HttpListener
        _listener?.Stop();
        _listener?.Close();
    }
    /// <summary>
    /// Xử lý WebSocket request mới
    /// </summary>
    private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
    {
        try
        {
            // Kiểm tra xem có phải WebSocket request không
            if (context.Request.IsWebSocketRequest)
            {
                // Chấp nhận WebSocket connection
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var socketId = Guid.NewGuid().ToString(); // Tạo unique ID cho connection
                // Lưu kết nối vào dictionary
                _connections[socketId] = webSocket;
                // Xử lý giao tiếp với client
                await HandleWebSocketCommunication(webSocket, socketId);
            }
            else
            {
                // Không phải WebSocket request
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
        catch (Exception ex)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            catch { }
        }
    }
    /// <summary>
    /// Xử lý giao tiếp với một WebSocket connection cụ thể
    /// </summary>
    private async Task HandleWebSocketCommunication(WebSocket webSocket, string socketId)
    {
        var buffer = new byte[1024 * 4]; // Buffer để nhận dữ liệu
        // Timer để gửi ping message định kỳ (giữ kết nối sống)
        var pingTimer = new Timer(async _ =>
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var pingMessage = JsonSerializer.Serialize(new { 
                        Type = "PING", 
                        Data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                        Timestamp = DateTime.UtcNow
                    });
                    var pingBuffer = Encoding.UTF8.GetBytes(pingMessage);
                    await webSocket.SendAsync(pingBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                }
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30)); // Ping mỗi 30 giây
        try
        {
            // Vòng lặp lắng nghe message từ client
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Nhận được text message
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await ProcessWebSocketMessage(socketId, message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Client muốn đóng kết nối
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
        }
        catch (Exception ex)
        {
        }
        finally
        {
            // Dọn dẹp khi kết nối đóng
            pingTimer?.Dispose();
            _connections.TryRemove(socketId, out _);
            // Nếu socket đang trong phòng nào đó thì rời phòng
            if (_socketToRoom.TryGetValue(socketId, out var roomCode))
            {
                try
                {
                    // Gọi RoomManagementService để xử lý việc rời phòng
                    if (_roomManagementService != null)
                    {
                        // Tìm userId của người chơi từ phòng
                        var room = await _roomManagementService.GetRoomAsync(roomCode);
                        if (room != null)
                        {
                            var player = room.Players.FirstOrDefault(p => p.SocketId == socketId);
                            if (player != null)
                            {
                                // Sử dụng LeaveRoomByUserIdAsync để đảm bảo xóa người chơi khỏi database
                                await _roomManagementService.LeaveRoomByUserIdAsync(player.UserId, roomCode);
                            }
                            else
                            {
                                await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
                            }
                        }
                        else
                        {
                            await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
                        }
                    }
                    else
                    {
                    }
                }
                catch (Exception ex)
                {
                }
                finally
                {
                    // Cleanup mapping
                    _socketToRoom.TryRemove(socketId, out _);
                }
            }
        }
    }
    /// <summary>
    /// Xử lý message nhận được từ WebSocket client
    /// </summary>
    private async Task ProcessWebSocketMessage(string socketId, string message)
    {
        try
        {
            // Parse JSON message
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            if (data == null) return;
            // Kiểm tra cả "event" và "type" field
            var eventName = data.GetValueOrDefault("event")?.ToString() ?? data.GetValueOrDefault("type")?.ToString();
            // Xử lý các event cơ bản (ping/pong)
            switch (eventName)
            {
                case "ping":
                    // Client gửi ping, trả lời pong
                    var pongMessage = JsonSerializer.Serialize(new { 
                        Type = "PONG", 
                        Data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
                        Timestamp = DateTime.UtcNow
                    });
                    var pongBuffer = Encoding.UTF8.GetBytes(pongMessage);
                    if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(pongBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    break;
                case "pong":
                    // Client trả lời ping của chúng ta
                    break;
                case "joinRoom":
                    // Xử lý join room event
                    await HandleJoinRoomEvent(socketId, data);
                    break;
                case "leaveRoom":
                    // Xử lý leave room event
                    await HandleLeaveRoomEvent(socketId, data);
                    break;
                case "request-players-update":
                    // Xử lý yêu cầu cập nhật danh sách người chơi
                    await HandleRequestPlayersUpdateEvent(socketId, data);
                    break;
                case "startGame":
                    // Xử lý start game event từ WebSocket
                    await HandleStartGameEvent(socketId, data);
                    break;
                // Các event khác sẽ được xử lý bởi các service khác
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
        }
    }
    // Reference tới RoomManagementSocketService để xử lý joinRoom
    private IRoomManagementSocketService? _roomManagementService;
    /// <summary>
    /// Thiết lập RoomManagementSocketService reference
    /// </summary>
    public void SetRoomManagementService(IRoomManagementSocketService roomManagementService)
    {
        _roomManagementService = roomManagementService;
    }
    /// <summary>
    /// Xử lý event joinRoom từ WebSocket client
    /// </summary>
    private async Task HandleJoinRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            // Lấy thông tin từ message - kiểm tra cả direct và nested trong "data"
            string? roomCode = null;
            string? username = null;
            string? userIdStr = null;
            // Kiểm tra direct fields trước
            roomCode = data.GetValueOrDefault("roomCode")?.ToString();
            username = data.GetValueOrDefault("username")?.ToString();
            userIdStr = data.GetValueOrDefault("userId")?.ToString();
            // Nếu không có, kiểm tra trong "data" nested
            if (string.IsNullOrEmpty(roomCode) && data.ContainsKey("data"))
            {
                var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data["data"].ToString() ?? "{}");
                if (nestedData != null)
                {
                    roomCode = nestedData.GetValueOrDefault("roomCode")?.ToString();
                    username = nestedData.GetValueOrDefault("username")?.ToString();
                    userIdStr = nestedData.GetValueOrDefault("userId")?.ToString();
                }
            }
            if (string.IsNullOrEmpty(roomCode))
            {
                return;
            }
            // Nếu không có username/userId, cần lấy từ session hoặc từ database
            if (string.IsNullOrEmpty(username) || !int.TryParse(userIdStr, out var userId))
            {
                return;
            }
            // Lưu mapping socketId -> roomCode
            _socketToRoom[socketId] = roomCode;
            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý event leaveRoom từ WebSocket client
    /// </summary>
    private async Task HandleLeaveRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            // Lấy thông tin từ message - kiểm tra cả direct và nested trong "data"
            string? roomCode = null;
            string? userIdStr = null;
            // Kiểm tra direct fields trước
            roomCode = data.GetValueOrDefault("roomCode")?.ToString();
            userIdStr = data.GetValueOrDefault("userId")?.ToString();
            // Nếu không có, kiểm tra trong "data" nested
            if ((string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(userIdStr)) && data.ContainsKey("data"))
            {
                var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data["data"].ToString() ?? "{}");
                if (nestedData != null)
                {
                    roomCode = roomCode ?? nestedData.GetValueOrDefault("roomCode")?.ToString();
                    userIdStr = userIdStr ?? nestedData.GetValueOrDefault("userId")?.ToString();
                }
            }
            // Nếu vẫn không có roomCode, thử lấy từ mapping
            if (string.IsNullOrEmpty(roomCode))
            {
                if (!_socketToRoom.TryGetValue(socketId, out roomCode))
                {
                    return;
                }
            }
            // Kiểm tra xem socket có thực sự đang ở trong phòng không
            if (!_socketToRoom.TryGetValue(socketId, out var currentRoom) || currentRoom != roomCode)
            {
                return;
            }
            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                // Nếu có userId, sử dụng LeaveRoomByUserIdAsync để đảm bảo xóa người chơi khỏi database
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
                {
                    await _roomManagementService.LeaveRoomByUserIdAsync(userId, roomCode);
                }
                else
                {
                    await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
                }
                // Xóa mapping socketId -> roomCode
                _socketToRoom.TryRemove(socketId, out _);
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý event request-players-update từ WebSocket client
    /// </summary>
    private async Task HandleRequestPlayersUpdateEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            // Lấy thông tin từ message - kiểm tra cả direct và nested trong "data"
            string? roomCode = null;
            // Kiểm tra direct fields trước
            roomCode = data.GetValueOrDefault("roomCode")?.ToString();
            // Nếu không có, kiểm tra trong "data" nested
            if (string.IsNullOrEmpty(roomCode) && data.ContainsKey("data"))
            {
                var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data["data"].ToString() ?? "{}");
                if (nestedData != null)
                {
                    roomCode = nestedData.GetValueOrDefault("roomCode")?.ToString();
                }
            }
            // Nếu vẫn không có roomCode, thử lấy từ mapping
            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }
            if (string.IsNullOrEmpty(roomCode))
            {
                return;
            }
            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                await _roomManagementService.RequestPlayersUpdateAsync(socketId, roomCode);
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
        }
    }

    /// <summary>
    /// Xử lý event startGame từ WebSocket client
    /// </summary>
    private async Task HandleStartGameEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🎮 [Backend] startGame received via WebSocket from socket: {socketId}");
            
            // Lấy thông tin từ message - kiểm tra cả direct và nested trong "data"
            string? roomCode = null;
            string? hostUserIdStr = null;
            
            // Kiểm tra direct fields trước
            roomCode = data.GetValueOrDefault("roomCode")?.ToString();
            hostUserIdStr = data.GetValueOrDefault("hostUserId")?.ToString();
            
            // Nếu không có, kiểm tra trong "data" nested
            if (string.IsNullOrEmpty(roomCode) && data.ContainsKey("data"))
            {
                var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data["data"].ToString() ?? "{}");
                if (nestedData != null)
                {
                    roomCode = roomCode ?? nestedData.GetValueOrDefault("roomCode")?.ToString();
                    hostUserIdStr = hostUserIdStr ?? nestedData.GetValueOrDefault("hostUserId")?.ToString();
                }
            }
            
            // Nếu vẫn không có roomCode, thử lấy từ mapping
            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }
            
            if (string.IsNullOrEmpty(roomCode) || !int.TryParse(hostUserIdStr, out var hostUserId))
            {
                Console.WriteLine($"❌ [Backend] Invalid startGame data: roomCode={roomCode}, hostUserId={hostUserIdStr}");
                
                // Gửi error response
                var errorMessage = JsonSerializer.Serialize(new { 
                    type = "error", 
                    data = new { message = "Invalid room code or host user ID" },
                    timestamp = DateTime.UtcNow
                });
                var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(errorBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                return;
            }
            
            Console.WriteLine($"🔍 [Backend] Processing startGame for room: {roomCode}, host: {hostUserId}");
            
            // Validate room exists và host permission thông qua RoomManagementService
            if (_roomManagementService != null)
            {
                var room = await _roomManagementService.GetRoomAsync(roomCode);
                if (room == null)
                {
                    Console.WriteLine($"❌ [Backend] Room {roomCode} not found");
                    
                    // Gửi error response
                    var errorMessage = JsonSerializer.Serialize(new { 
                        type = "error", 
                        data = new { message = "Room not found" },
                        timestamp = DateTime.UtcNow
                    });
                    var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                    if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(errorBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    return;
                }
                
                // Kiểm tra host permission
                var hostPlayer = room.Players.FirstOrDefault(p => p.UserId == hostUserId && p.IsHost);
                if (hostPlayer == null)
                {
                    Console.WriteLine($"❌ [Backend] User {hostUserId} is not host of room {roomCode}");
                    
                    // Gửi error response
                    var errorMessage = JsonSerializer.Serialize(new { 
                        type = "error", 
                        data = new { message = "Unauthorized: Only host can start game" },
                        timestamp = DateTime.UtcNow
                    });
                    var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                    if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(errorBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    return;
                }
                
                // Kiểm tra minimum players
                if (room.Players.Count < 1)
                {
                    Console.WriteLine($"❌ [Backend] Not enough players in room {roomCode}");
                    
                    // Gửi error response
                    var errorMessage = JsonSerializer.Serialize(new { 
                        type = "error", 
                        data = new { message = "Need at least 1 player to start game" },
                        timestamp = DateTime.UtcNow
                    });
                    var errorBuffer = Encoding.UTF8.GetBytes(errorMessage);
                    if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(errorBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    return;
                }
                
                Console.WriteLine($"✅ [Backend] Room validation passed. Players in room: {room.Players.Count}");
                
                // 🚨 CRITICAL: Broadcast game-started event tới TẤT CẢ players trong room
                var gameStartData = new
                {
                    roomCode = roomCode,
                    gameData = new
                    {
                        roomCode = roomCode,
                        selectedTopicIds = data.GetValueOrDefault("selectedTopicIds") ?? new List<int>(),
                        questionCount = data.GetValueOrDefault("questionCount") ?? 10,
                        timeLimit = data.GetValueOrDefault("timeLimit") ?? 30,
                        hostUserId = hostUserId,
                        startTime = DateTime.UtcNow.ToString("O"),
                        players = room.Players.Select(p => new { 
                            userId = p.UserId, 
                            username = p.Username, 
                            isHost = p.IsHost 
                        }).ToList()
                    }
                };
                
                // Broadcast thông qua RoomManagement để đảm bảo gửi tới tất cả players
                await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "game-started", gameStartData);
                
                Console.WriteLine($"📡 [Backend] game-started broadcasted to room {roomCode}");
                Console.WriteLine($"📡 [Backend] Players notified: {string.Join(",", room.Players.Select(p => p.Username))}");
                
                // Cập nhật game state ở backend
                room.GameState = "starting";
            }
            else
            {
                Console.WriteLine($"❌ [Backend] RoomManagementService not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [Backend] Error handling startGame: {ex.Message}");
        }
    }
}
