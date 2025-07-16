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
    // Dictionary lưu trữ tất cả các kết nối WebSocket hiện tại
    // Key: socketId (unique), Value: WebSocket connection
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    
    // Dictionary ánh xạ socketId với roomCode
    // Key: socketId, Value: roomCode mà socket đang tham gia
    private readonly ConcurrentDictionary<string, string> _socketToRoom = new();
    
    // HttpListener để lắng nghe các WebSocket request
    private HttpListener? _listener;

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
        
        Console.WriteLine($"[SOCKET] WebSocket server started on ws://localhost:{port}");
        Console.WriteLine($"[SOCKET] Accepting WebSocket connections...");
        
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
                    Console.WriteLine($"[SOCKET] Error: {ex.Message}");
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
        Console.WriteLine("[SOCKET] Stopping WebSocket server...");
        
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
        
        Console.WriteLine("[SOCKET] WebSocket service stopped");
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
                Console.WriteLine($"[SOCKET] Accepting WebSocket connection from {context.Request.RemoteEndPoint}");
                
                // Chấp nhận WebSocket connection
                var webSocketContext = await context.AcceptWebSocketAsync(null);
                var webSocket = webSocketContext.WebSocket;
                var socketId = Guid.NewGuid().ToString(); // Tạo unique ID cho connection
                
                // Lưu connection vào dictionary
                _connections[socketId] = webSocket;
                Console.WriteLine($"[SOCKET] New WebSocket connection established: {socketId}");
                
                // Xử lý giao tiếp với client
                await HandleWebSocketCommunication(webSocket, socketId);
            }
            else
            {
                // Không phải WebSocket request
                Console.WriteLine($"[SOCKET] Non-WebSocket request from {context.Request.RemoteEndPoint}");
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Error handling WebSocket request: {ex.Message}");
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
                        eventName = "ping", 
                        data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } 
                    });
                    var pingBuffer = Encoding.UTF8.GetBytes(pingMessage);
                    await webSocket.SendAsync(pingBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SOCKET] Ping failed for {socketId}: {ex.Message}");
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
                    Console.WriteLine($"[SOCKET] WebSocket close received from {socketId}");
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"[SOCKET] WebSocket exception for {socketId}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Unexpected error for {socketId}: {ex.Message}");
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
                // Lưu ý: Trong thực tế cần inject IRoomManagementSocketService
                // Hiện tại chỉ cleanup mapping
                _socketToRoom.TryRemove(socketId, out _);
                Console.WriteLine($"[SOCKET] Socket {socketId} removed from room {roomCode} mapping");
            }
            Console.WriteLine($"[SOCKET] WebSocket disconnected: {socketId}");
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
            
            var eventName = data.GetValueOrDefault("event")?.ToString();
            Console.WriteLine($"[SOCKET] Received event: {eventName} from {socketId}");
            
            // Xử lý các event cơ bản (ping/pong)
            switch (eventName)
            {
                case "ping":
                    // Client gửi ping, trả lời pong
                    var pongMessage = JsonSerializer.Serialize(new { 
                        eventName = "pong", 
                        data = new { timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() } 
                    });
                    var pongBuffer = Encoding.UTF8.GetBytes(pongMessage);
                    if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(pongBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    break;
                    
                case "pong":
                    // Client trả lời ping của chúng ta
                    Console.WriteLine($"[SOCKET] Pong received from {socketId}");
                    break;
                    
                // Các event khác sẽ được xử lý bởi các service khác
                // Ví dụ: join-room, leave-room, submit-answer, etc.
                default:
                    Console.WriteLine($"[SOCKET] Event {eventName} will be handled by other services");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SOCKET] Error processing message: {ex.Message}");
        }
    }
}