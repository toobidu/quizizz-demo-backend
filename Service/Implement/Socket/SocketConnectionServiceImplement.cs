using ConsoleApp1.Service.Interface.Socket;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

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
        
        Console.WriteLine($"[SOCKET] Máy chủ WebSocket đã khởi động tại ws://localhost:{port}");
        Console.WriteLine($"[SOCKET] Đang chấp nhận các kết nối WebSocket...");
        
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
                    Console.WriteLine($"[SOCKET] Lỗi: {ex.Message}");
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
        Console.WriteLine("[SOCKET] Đang dừng máy chủ WebSocket...");
        
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
        
        Console.WriteLine("[SOCKET] Dịch vụ WebSocket đã dừng");
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
                Console.WriteLine($"[SOCKET] Đang chấp nhận kết nối WebSocket từ {context.Request.RemoteEndPoint}");
                
                // Chấp nhận WebSocket connection
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var socketId = Guid.NewGuid().ToString(); // Tạo unique ID cho connection
                
                // Lưu kết nối vào dictionary
                _connections[socketId] = webSocket;
                Console.WriteLine($"[SOCKET] Kết nối WebSocket mới đã được thiết lập: {socketId}");
                
                // Xử lý giao tiếp với client
                await HandleWebSocketCommunication(webSocket, socketId);
            }
            else
            {
                // Không phải WebSocket request
                Console.WriteLine($"[SOCKET] Yêu cầu không phải WebSocket từ {context.Request.RemoteEndPoint}");
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Lỗi xử lý yêu cầu WebSocket: {ex.Message}");
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
                    Console.WriteLine($"[SOCKET] Ping thất bại cho {socketId}: {ex.Message}");
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
                    Console.WriteLine($"[SOCKET] Nhận yêu cầu đóng WebSocket từ {socketId}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[SOCKET] Ngoại lệ WebSocket cho {socketId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Lỗi không mong muốn cho {socketId}: {ex.Message}");
        }
        finally
        {
            // Dọn dẹp khi kết nối đóng
            pingTimer?.Dispose();
            _connections.TryRemove(socketId, out _);
            
            // Nếu socket đang trong phòng nào đó thì rời phòng
            if (_socketToRoom.TryGetValue(socketId, out var roomCode))
            {
                // Gọi service khác để xử lý việc rời phòng
                // Hiện tại chỉ cleanup mapping
                _socketToRoom.TryRemove(socketId, out _);
                Console.WriteLine($"[SOCKET] Socket {socketId} đã được xóa khỏi ánh xạ phòng {roomCode}");
            }
            Console.WriteLine($"[SOCKET] WebSocket đã ngắt kết nối: {socketId}");
        }
    }
    
    /// <summary>
    /// Xử lý message nhận được từ WebSocket client
    /// </summary>
    private async Task ProcessWebSocketMessage(string socketId, string message)
    {
        try
        {
            Console.WriteLine($"[SOCKET] Raw message: {message}");
            
            // Parse JSON message
            var data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            if (data == null) return;
            
            // Kiểm tra cả "event" và "type" field
            var eventName = data.GetValueOrDefault("event")?.ToString() ?? data.GetValueOrDefault("type")?.ToString();
            Console.WriteLine($"[SOCKET] Đã nhận sự kiện: {eventName} từ {socketId}");
            
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
                    Console.WriteLine($"[SOCKET] Đã nhận Pong từ {socketId}");
                    break;
                    
                case "joinRoom":
                    // Xử lý join room event
                    await HandleJoinRoomEvent(socketId, data);
                    break;
                    
                // Các event khác sẽ được xử lý bởi các service khác
                default:
                    Console.WriteLine($"[SOCKET] Sự kiện {eventName} sẽ được xử lý bởi các dịch vụ khác");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Lỗi xử lý tin nhắn: {ex.Message}");
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
            Console.WriteLine($"[SOCKET] HandleJoinRoomEvent data: {JsonSerializer.Serialize(data)}");
            
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
            
            Console.WriteLine($"[SOCKET] Parsed: roomCode={roomCode}, username={username}, userId={userIdStr}");
            
            if (string.IsNullOrEmpty(roomCode))
            {
                Console.WriteLine($"[SOCKET] Thiếu roomCode từ {socketId}");
                return;
            }
            
            // Nếu không có username/userId, cần lấy từ session hoặc từ database
            if (string.IsNullOrEmpty(username) || !int.TryParse(userIdStr, out var userId))
            {
                Console.WriteLine($"[SOCKET] Thiếu thông tin user, không thể join room qua WebSocket");
                Console.WriteLine($"[SOCKET] User cần join qua HTTP API trước, sau đó WebSocket sẽ tự động sync");
                return;
            }
            
            // Lưu mapping socketId -> roomCode
            _socketToRoom[socketId] = roomCode;
            
            Console.WriteLine($"[SOCKET] Xử lý joinRoom: socketId={socketId}, roomCode={roomCode}, username={username}, userId={userId}");
            
            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
            }
            else
            {
                Console.WriteLine($"[SOCKET] RoomManagementService chưa được thiết lập!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Lỗi xử lý joinRoom: {ex.Message}");
        }
    }
}