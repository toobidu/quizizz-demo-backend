using ConsoleApp1.Service.Interface.Socket;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleApp1.Config;

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
    // Dictionary ánh xạ socketId với userId để track users
    private readonly ConcurrentDictionary<string, int> _socketToUserId;
    // Dictionary theo dõi last pong time cho mỗi connection
    private readonly ConcurrentDictionary<string, DateTime> _lastPongReceived;
    // HttpListener để lắng nghe các WebSocket request
    private HttpListener? _listener;
    // Reference tới RoomManagementSocketService để xử lý joinRoom
    private IRoomManagementSocketService? _roomManagementService;

    // ✅ TĂNG PONG TIMEOUT LÊN 120 GIÂY
    private const int PONG_TIMEOUT_SECONDS = 120;
    private const int PING_INTERVAL_SECONDS = 30;

    /// <summary>
    /// Constructor nhận shared dictionaries
    /// </summary>
    public SocketConnectionServiceImplement(
        ConcurrentDictionary<string, WebSocket> connections,
        ConcurrentDictionary<string, string> socketToRoom)
    {
        _connections = connections;
        _socketToRoom = socketToRoom;
        _socketToUserId = new ConcurrentDictionary<string, int>();
        _lastPongReceived = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Constructor mặc định (backward compatibility)
    /// </summary>
    public SocketConnectionServiceImplement()
    {
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _socketToRoom = new ConcurrentDictionary<string, string>();
        _socketToUserId = new ConcurrentDictionary<string, int>();
        _lastPongReceived = new ConcurrentDictionary<string, DateTime>();
    }

    /// <summary>
    /// Thiết lập RoomManagementSocketService reference
    /// </summary>
    public void SetRoomManagementService(IRoomManagementSocketService roomManagementService)
    {
        _roomManagementService = roomManagementService;
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
        
        Console.WriteLine($"🔌 [SocketConnectionService] WebSocket server started on port {port}");

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
                    Console.WriteLine("🛑 [SocketConnectionService] Server stopped listening");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Error in listener loop: {ex.Message}");
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
        Console.WriteLine("🛑 [SocketConnectionService] Stopping WebSocket server...");
        
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
        
        Console.WriteLine("✅ [SocketConnectionService] WebSocket server stopped");
    }

    /// <summary>
    /// Xử lý WebSocket request mới với validation
    /// </summary>
    private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
    {
        var clientIP = context.Request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var userAgent = context.Request.Headers["User-Agent"] ?? "unknown";
        
        Console.WriteLine($"📡 [WebSocket] New request from {clientIP} to path: {path}");
        Console.WriteLine($"📡 [WebSocket] User-Agent: {userAgent}");
        
        try
        {
            // ✅ KIỂM TRA ĐÚNG WEBSOCKET REQUEST
            if (!context.Request.IsWebSocketRequest)
            {
                Console.WriteLine($"❌ [WebSocket] Not a WebSocket request from {clientIP}");
                context.Response.StatusCode = 400;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("WebSocket connection required"));
                context.Response.Close();
                return;
            }

            // ✅ VALIDATE URL PATH (optional - có thể skip nếu không cần)
            if (!IsValidWebSocketPath(path))
            {
                Console.WriteLine($"❌ [WebSocket] Invalid path '{path}' from {clientIP}");
                context.Response.StatusCode = 404;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Path '{path}' not supported"));
                context.Response.Close();
                return;
            }

            // ✅ EXTRACT ROOMCODE TỪ PATH (nếu có pattern như /waiting-room/{roomCode})
            var roomCodeFromPath = ExtractRoomCodeFromPath(path);
            if (!string.IsNullOrEmpty(roomCodeFromPath))
            {
                Console.WriteLine($"🏠 [WebSocket] Room code from path: {roomCodeFromPath}");
            }

            // ✅ CHẤP NHẬN WEBSOCKET CONNECTION
            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var socketId = Guid.NewGuid().ToString();

            Console.WriteLine($"🔗 [WebSocket] Connection accepted: {socketId} from {clientIP}");
            Console.WriteLine($"🔗 [WebSocket] Connection state: {webSocket.State}");

            // Lưu metadata
            var connectionInfo = new ConnectionMetadata 
            {
                SocketId = socketId,
                ClientIP = clientIP,
                ConnectedAt = DateTime.UtcNow,
                Path = path,
                RoomCodeFromPath = roomCodeFromPath,
                UserAgent = userAgent
            };

            // Lưu kết nối vào dictionary
            _connections[socketId] = webSocket;

            // Xử lý giao tiếp với client
            await HandleWebSocketCommunication(webSocket, socketId, connectionInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [WebSocket] Error handling request from {clientIP}: {ex.Message}");
            Console.WriteLine($"❌ [WebSocket] Stack trace: {ex.StackTrace}");
            try
            {
                context.Response.StatusCode = 500;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Server error: {ex.Message}"));
                context.Response.Close();
            }
            catch (Exception closeEx)
            {
                Console.WriteLine($"❌ [WebSocket] Error closing response: {closeEx.Message}");
            }
        }
    }

    /// <summary>
    /// Validate WebSocket path
    /// </summary>
    private bool IsValidWebSocketPath(string path)
    {
        // Cho phép mọi path, hoặc implement logic cụ thể
        var validPaths = new[] { "/", "/ws", "/websocket" };
        var validPatterns = new[] { @"^/waiting-room/[A-Za-z0-9]+$" };
        
        // Check exact paths
        if (validPaths.Contains(path.ToLower()))
            return true;
            
        // Check patterns
        foreach (var pattern in validPatterns)
        {
            if (Regex.IsMatch(path, pattern))
                return true;
        }
        
        // Allow all for now
        return true;
    }

    /// <summary>
    /// Extract room code from path như /waiting-room/{roomCode}
    /// </summary>
    private string? ExtractRoomCodeFromPath(string path)
    {
        var match = Regex.Match(path, @"^/waiting-room/([A-Za-z0-9]+)$");
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>
    /// Enhanced WebSocket communication với better logging
    /// </summary>
    private async Task HandleWebSocketCommunication(WebSocket webSocket, string socketId, ConnectionMetadata metadata)
    {
        var buffer = new byte[1024 * 4];
        var lastPongReceived = DateTime.UtcNow;
        var pingCount = 0;
        var messageCount = 0;

        Console.WriteLine($"🔄 [WebSocket] Starting communication loop for {socketId}");

        // ✅ ENHANCED PING TIMER với timeout detection
        var pingTimer = new Timer(async _ =>
        {
            if (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    pingCount++;
                    
                    // ✅ CHECK PONG TIMEOUT (120 seconds)
                    var timeSinceLastPong = DateTime.UtcNow - lastPongReceived;
                    if (timeSinceLastPong.TotalSeconds > PONG_TIMEOUT_SECONDS)
                    {
                        Console.WriteLine($"⏰ [WebSocket] Pong timeout for {socketId} ({timeSinceLastPong.TotalSeconds:F1}s)");
                        Console.WriteLine($"🔌 [WebSocket] Closing connection {socketId} due to pong timeout");
                        
                        await webSocket.CloseAsync(
                            WebSocketCloseStatus.PolicyViolation, 
                            "Pong timeout", 
                            CancellationToken.None
                        );
                        return;
                    }

                    var pingMessage = JsonSerializer.Serialize(new
                    {
                        type = "ping",
                        data = new { 
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            pingCount = pingCount
                        },
                        timestamp = DateTime.UtcNow
                    });
                    
                    var pingBuffer = Encoding.UTF8.GetBytes(pingMessage);
                    await webSocket.SendAsync(pingBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    
                    Console.WriteLine($"🏓 [WebSocket] Sent ping #{pingCount} to {socketId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [WebSocket] Error sending ping to {socketId}: {ex.Message}");
                    Console.WriteLine($"🔌 [WebSocket] Will close connection {socketId} due to ping error");
                }
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

        try
        {
            // ✅ MAIN COMMUNICATION LOOP với detailed logging
            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    Console.WriteLine($"👂 [WebSocket] Waiting for message from {socketId}...");
                    
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    
                    Console.WriteLine($"📨 [WebSocket] Received from {socketId}: Type={result.MessageType}, Count={result.Count}, EndOfMessage={result.EndOfMessage}");

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageCount++;
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        
                        Console.WriteLine($"📥 [WebSocket] Message #{messageCount} from {socketId}: {message}");
                        
                        // Track pong responses và heartbeat
                        if (message.Contains("\"type\":\"pong\"") || message.Contains("\"event\":\"pong\"") || 
                            message.Contains("\"event\":\"heartbeat\"") || message.Contains("\"type\":\"heartbeat\""))
                        {
                            lastPongReceived = DateTime.UtcNow;
                            _lastPongReceived[socketId] = DateTime.UtcNow;
                            Console.WriteLine($"🏓 [WebSocket] Received pong/heartbeat from {socketId}");
                        }
                        
                        await ProcessWebSocketMessage(socketId, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"🔌 [WebSocket] Client {socketId} requested close: {result.CloseStatus} - {result.CloseStatusDescription}");
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledged", CancellationToken.None);
                        break;
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        Console.WriteLine($"📦 [WebSocket] Received binary data from {socketId} (not supported)");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"⏹️ [WebSocket] Operation cancelled for {socketId}");
                    break;
                }
                catch (WebSocketException wsEx)
                {
                    Console.WriteLine($"🔌 [WebSocket] WebSocket exception for {socketId}: {wsEx.Message}");
                    Console.WriteLine($"🔌 [WebSocket] WebSocket error code: {wsEx.WebSocketErrorCode}");
                    Console.WriteLine($"🔌 [WebSocket] Native error code: {wsEx.NativeErrorCode}");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [WebSocket] Unexpected error for {socketId}: {ex.Message}");
            Console.WriteLine($"❌ [WebSocket] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"❌ [WebSocket] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            // ✅ DETAILED CLEANUP LOGGING
            var duration = DateTime.UtcNow - metadata.ConnectedAt;
            Console.WriteLine($"🔌 [WebSocket] Connection {socketId} closing after {duration.TotalMinutes:F1} minutes");
            Console.WriteLine($"📊 [WebSocket] Stats for {socketId}: {messageCount} messages, {pingCount} pings");
            Console.WriteLine($"🔌 [WebSocket] Final state: {webSocket.State}");
            
            pingTimer?.Dispose();
            _connections.TryRemove(socketId, out _);

            // ✅ ENHANCED ROOM CLEANUP
            if (_socketToRoom.TryGetValue(socketId, out var roomCode))
            {
                Console.WriteLine($"🚪 [WebSocket] Cleaning up room membership for {socketId} in room {roomCode}");
                
                try
                {
                    if (_roomManagementService != null)
                    {
                        var room = await _roomManagementService.GetRoomAsync(roomCode);
                        if (room != null)
                        {
                            var player = room.Players.FirstOrDefault(p => p.SocketId == socketId);
                            if (player != null)
                            {
                                Console.WriteLine($"🚪 [WebSocket] Player {player.Username} (ID: {player.UserId}) leaving room {roomCode} due to disconnect");
                                await _roomManagementService.LeaveRoomByUserIdAsync(player.UserId, roomCode);
                            }
                            else
                            {
                                Console.WriteLine($"⚠️ [WebSocket] Player not found in room {roomCode} for socket {socketId}");
                                await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ [WebSocket] Room {roomCode} not found for socket {socketId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ [WebSocket] RoomManagementService not available for cleanup");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [WebSocket] Error during room cleanup for {socketId}: {ex.Message}");
                }
                finally
                {
                    _socketToRoom.TryRemove(socketId, out _);
                    _socketToUserId.TryRemove(socketId, out _);
                    _lastPongReceived.TryRemove(socketId, out _);
                    Console.WriteLine($"🧹 [WebSocket] Removed socket-to-room and user mappings for {socketId}");
                }
            }
            
            Console.WriteLine($"✅ [WebSocket] Cleanup completed for {socketId}");
        }
    }

    /// <summary>
    /// Xử lý message nhận được từ WebSocket client
    /// </summary>
    private async Task ProcessWebSocketMessage(string socketId, string message)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            Console.WriteLine($"📥 [Message] Processing from {socketId}: {message}");

            if (string.IsNullOrWhiteSpace(message))
            {
                Console.WriteLine($"⚠️ [Message] Empty message from {socketId}");
                await SendAckResponse(socketId, "unknown", false, "Empty message");
                return;
            }

            Dictionary<string, object>? data;
            try
            {
                data = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"❌ [Message] Invalid JSON from {socketId}: {jsonEx.Message}");
                await SendAckResponse(socketId, "unknown", false, "Invalid JSON format");
                return;
            }

            if (data == null)
            {
                Console.WriteLine($"⚠️ [Message] Null data from {socketId}");
                await SendAckResponse(socketId, "unknown", false, "Null message data");
                return;
            }

            var eventName = data.GetValueOrDefault("event")?.ToString() ?? data.GetValueOrDefault("type")?.ToString();
            
            if (string.IsNullOrEmpty(eventName))
            {
                Console.WriteLine($"⚠️ [Message] Missing event name from {socketId}");
                await SendAckResponse(socketId, "unknown", false, "Missing event name");
                return;
            }

            Console.WriteLine($"🎯 [Message] Event '{eventName}' from {socketId}");

            // ✅ LOG VALIDATION CHO CÁC EVENTS QUAN TRỌNG
            switch (eventName.ToLower())
            {
                case "join-room":
                case "joinroom":
                    await ValidateAndHandleJoinRoom(socketId, data, eventName);
                    break;
                case "leave-room":
                case "leaveroom":
                    await HandleLeaveRoomEvent(socketId, data);
                    break;
                case "start-game":
                case "startgame":
                    await HandleStartGameEvent(socketId, data);
                    break;
                case "player-ready":
                case "playerready":
                    await HandlePlayerReadyEvent(socketId, data);
                    break;
                case "request-players-update":
                    await HandleRequestPlayersUpdateEvent(socketId, data);
                    break;
                case "ping":
                    await HandlePingEvent(socketId);
                    break;
                case "pong":
                    Console.WriteLine($"🏓 [Message] Pong received from {socketId}");
                    break;
                case "heartbeat":
                    await HandleHeartbeatEvent(socketId, data);
                    break;
                case "player-left":
                    await HandlePlayerLeftEvent(socketId, data);
                    break;
                default:
                    Console.WriteLine($"⚠️ [Message] Unhandled event '{eventName}' from {socketId}");
                    await SendAckResponse(socketId, eventName, false, "Event not supported");
                    break;
            }
            
            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"✅ [Message] Processed '{eventName}' in {duration.TotalMilliseconds:F1}ms");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"❌ [Message] Error processing message from {socketId} after {duration.TotalMilliseconds:F1}ms: {ex.Message}");
            Console.WriteLine($"❌ [Message] Exception type: {ex.GetType().Name}");
            Console.WriteLine($"❌ [Message] Stack trace: {ex.StackTrace}");
            
            try
            {
                await SendAckResponse(socketId, "unknown", false, "Internal server error");
            }
            catch (Exception ackEx)
            {
                Console.WriteLine($"❌ [Message] Failed to send error ACK to {socketId}: {ackEx.Message}");
            }
        }
    }

    /// <summary>
    /// Validate và handle join-room với enhanced logging
    /// </summary>
    private async Task ValidateAndHandleJoinRoom(string socketId, Dictionary<string, object> data, string eventName)
    {
        Console.WriteLine($"🚪 [JoinRoom] Processing join-room from {socketId}");
        
        // Extract data với validation
        var roomCode = ExtractStringValue(data, "roomCode");
        var username = ExtractStringValue(data, "username"); 
        var userIdStr = ExtractStringValue(data, "userId");
        
        Console.WriteLine($"🚪 [JoinRoom] Data - roomCode: {roomCode}, username: {username}, userId: {userIdStr}");
        
        // Validation
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            Console.WriteLine($"❌ [JoinRoom] Missing roomCode from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Missing room code");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(username))
        {
            Console.WriteLine($"❌ [JoinRoom] Missing username from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Missing username");
            return;
        }
        
        if (!int.TryParse(userIdStr, out var userId))
        {
            Console.WriteLine($"❌ [JoinRoom] Invalid userId '{userIdStr}' from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Invalid user ID");
            return;
        }
        
        // ✅ VALIDATE ROOMCODE FORMAT
        if (!IsValidRoomCode(roomCode))
        {
            Console.WriteLine($"❌ [JoinRoom] Invalid roomCode format '{roomCode}' from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Invalid room code format");
            return;
        }
        
        Console.WriteLine($"✅ [JoinRoom] Validation passed for {username} joining {roomCode}");
        
        await HandleJoinRoomEvent(socketId, data);
    }

    /// <summary>
    /// Validate room code format
    /// </summary>
    private bool IsValidRoomCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return false;
        if (roomCode.Length < RoomManagementConstants.Limits.MinRoomCodeLength) return false;
        if (roomCode.Length > RoomManagementConstants.Limits.MaxRoomCodeLength) return false;
        
        // Chỉ cho phép alphanumeric
        return roomCode.All(char.IsLetterOrDigit);
    }

    /// <summary>
    /// Extract string value với fallback cho nested data
    /// </summary>
    private string? ExtractStringValue(Dictionary<string, object> data, string key)
    {
        // Direct key
        var directValue = data.GetValueOrDefault(key)?.ToString();
        if (!string.IsNullOrEmpty(directValue)) return directValue;
        
        // Nested trong "data"
        if (data.ContainsKey("data"))
        {
            try
            {
                var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(data["data"].ToString() ?? "{}");
                return nestedData?.GetValueOrDefault(key)?.ToString();
            }
            catch
            {
                return null;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Xử lý event join-room từ WebSocket client
    /// </summary>
    private async Task HandleJoinRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] join-room received from {socketId}");
            
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
                await SendAckResponse(socketId, "join-room", false, "Missing room code");
                return;
            }

            // Nếu không có username/userId, cần lấy từ session hoặc từ database
            if (string.IsNullOrEmpty(username) || !int.TryParse(userIdStr, out var userId))
            {
                await SendAckResponse(socketId, "join-room", false, "Missing username or invalid userId");
                return;
            }

            // Lưu mapping socketId -> roomCode và socketId -> userId
            _socketToRoom[socketId] = roomCode;
            _socketToUserId[socketId] = userId;

            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
                await SendAckResponse(socketId, "join-room", true, "Successfully joined room");
                
                Console.WriteLine($"✅ [SocketConnectionService] {username} joined room {roomCode}");
                
                // Gửi sự kiện players-updated sau khi join thành công
                try
                {
                    var room = await _roomManagementService.GetRoomAsync(roomCode);
                    if (room != null)
                    {
                        // Broadcast player-joined event
                        await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "player-joined", new
                        {
                            userId = userId,
                            username = username,
                            isHost = room.Players.Any(p => p.UserId == userId && p.IsHost),
                            roomCode = roomCode,
                            timestamp = DateTime.UtcNow
                        });
                        
                        // Broadcast players-updated event để đồng bộ danh sách
                        await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "players-updated", new
                        {
                            players = room.Players.Select(p => new { 
                                userId = p.UserId, 
                                username = p.Username, 
                                isHost = p.IsHost,
                                status = p.Status ?? "waiting"
                            }).ToList(),
                            roomCode = roomCode
                        });
                        
                        Console.WriteLine($"📡 [SocketConnectionService] Broadcasted player-joined and players-updated for {username} in room {roomCode}");
                    }
                }
                catch (Exception broadcastEx)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Error broadcasting join events: {broadcastEx.Message}");
                }
            }
            else
            {
                await SendAckResponse(socketId, "join-room", false, "Room service not available");
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling join-room: {ex.Message}");
            await SendAckResponse(socketId, "join-room", false, "Internal server error");
        }
    }

    /// <summary>
    /// Xử lý event leave-room từ WebSocket client
    /// </summary>
    private async Task HandleLeaveRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] leave-room received from {socketId}");
            
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
                    await SendAckResponse(socketId, "leave-room", false, "Room code not found");
                    return;
                }
            }

            // Kiểm tra xem socket có thực sự đang ở trong phòng không
            if (!_socketToRoom.TryGetValue(socketId, out var currentRoom) || currentRoom != roomCode)
            {
                await SendAckResponse(socketId, "leave-room", false, "Not in specified room");
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

                // Xóa mapping
                _socketToRoom.TryRemove(socketId, out _);
                _socketToUserId.TryRemove(socketId, out _);

                await SendAckResponse(socketId, "leave-room", true, "Successfully left room");
                
                // Gửi players-updated event sau khi leave
                try
                {
                    var room = await _roomManagementService.GetRoomAsync(roomCode);
                    if (room != null)
                    {
                        await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "players-updated", new
                        {
                            players = room.Players.Select(p => new { 
                                userId = p.UserId, 
                                username = p.Username, 
                                isHost = p.IsHost,
                                status = p.Status ?? "waiting"
                            }).ToList(),
                            roomCode = roomCode
                        });
                        
                        Console.WriteLine($"📡 [SocketConnectionService] Broadcasted players-updated after leave in room {roomCode}");
                    }
                }
                catch (Exception broadcastEx)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Error broadcasting leave events: {broadcastEx.Message}");
                }
                
                Console.WriteLine($"✅ [SocketConnectionService] Socket {socketId} left room {roomCode}");
            }
            else
            {
                await SendAckResponse(socketId, "leave-room", false, "Room service not available");
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling leave-room: {ex.Message}");
            await SendAckResponse(socketId, "leave-room", false, "Internal server error");
        }
    }

    /// <summary>
    /// Xử lý event request-players-update từ WebSocket client
    /// </summary>
    private async Task HandleRequestPlayersUpdateEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🔄 [SocketConnectionService] request-players-update received from {socketId}");

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
                await SendAckResponse(socketId, "request-players-update", false, "Room code not found");
                return;
            }

            // Sử dụng shared RoomManagementSocketService
            if (_roomManagementService != null)
            {
                await _roomManagementService.RequestPlayersUpdateAsync(socketId, roomCode);
                await SendAckResponse(socketId, "request-players-update", true, "Players update sent");
            }
            else
            {
                await SendAckResponse(socketId, "request-players-update", false, "Room service not available");
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling request-players-update: {ex.Message}");
            await SendAckResponse(socketId, "request-players-update", false, "Internal server error");
        }
    }

    /// <summary>
    /// Xử lý ping event từ client để maintain connection
    /// </summary>
    private async Task HandlePingEvent(string socketId)
    {
        try
        {
            Console.WriteLine($"🏓 [SocketConnectionService] Ping received from {socketId}");
            
            // Send pong response
            var pongResponse = new
            {
                type = "pong",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                socketId = socketId
            };

            await SendMessageToSocketAsync(socketId, pongResponse);
            Console.WriteLine($"🏓 [SocketConnectionService] Pong sent to {socketId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in HandlePingEvent: {ex.Message}");
        }
    }

    /// <summary>
    /// Xử lý heartbeat event từ client để maintain connection
    /// </summary>
    private async Task HandleHeartbeatEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"💓 [SocketConnectionService] Heartbeat received from {socketId}");
            
            // Cập nhật last heartbeat time
            _lastPongReceived[socketId] = DateTime.UtcNow;
            
            // Send heartbeat response với status ok
            var heartbeatResponse = new
            {
                @event = "heartbeat",
                data = new
                {
                    status = "ok",
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    socketId = socketId
                },
                timestamp = DateTime.UtcNow
            };

            await SendMessageToSocketAsync(socketId, heartbeatResponse);
            Console.WriteLine($"💓 [SocketConnectionService] Heartbeat response sent to {socketId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in HandleHeartbeatEvent: {ex.Message}");
        }
    }

    /// <summary>
    /// Xử lý player-left event từ client
    /// </summary>
    private async Task HandlePlayerLeftEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] player-left received from {socketId}");
            
            // Lấy thông tin từ message
            var roomCode = ExtractStringValue(data, "roomCode");
            var userIdStr = ExtractStringValue(data, "userId");
            var username = ExtractStringValue(data, "username");
            
            // Nếu không có roomCode, lấy từ mapping
            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }
            
            if (string.IsNullOrEmpty(roomCode))
            {
                await SendAckResponse(socketId, "player-left", false, "Room code not found");
                return;
            }
            
            Console.WriteLine($"🚪 [SocketConnectionService] Processing player-left: roomCode={roomCode}, userId={userIdStr}, username={username}");
            
            if (_roomManagementService != null)
            {
                // Nếu có userId, sử dụng để xóa player khỏi database
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
                {
                    await _roomManagementService.LeaveRoomByUserIdAsync(userId, roomCode);
                    
                    // Broadcast player-left event cho các players khác
                    await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "player-left", new
                    {
                        userId = userId,
                        username = username ?? "Unknown",
                        roomCode = roomCode,
                        timestamp = DateTime.UtcNow
                    });
                    
                    // Gửi players-updated event để đồng bộ danh sách
                    var room = await _roomManagementService.GetRoomAsync(roomCode);
                    if (room != null)
                    {
                        await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "players-updated", new
                        {
                            players = room.Players.Select(p => new { 
                                userId = p.UserId, 
                                username = p.Username, 
                                isHost = p.IsHost,
                                status = p.Status
                            }).ToList(),
                            roomCode = roomCode
                        });
                    }
                }
                else
                {
                    await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
                }
                
                // Xóa mapping
                _socketToRoom.TryRemove(socketId, out _);
                _socketToUserId.TryRemove(socketId, out _);
                
                await SendAckResponse(socketId, "player-left", true, "Player left successfully");
                Console.WriteLine($"✅ [SocketConnectionService] Player left room {roomCode} successfully");
            }
            else
            {
                await SendAckResponse(socketId, "player-left", false, "Room service not available");
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling player-left: {ex.Message}");
            await SendAckResponse(socketId, "player-left", false, "Internal server error");
        }
    }

    /// <summary>
    /// Xử lý event start-game từ WebSocket client
    /// </summary>
    private async Task HandleStartGameEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🎮 [SocketConnectionService] start-game received from {socketId}");
            
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
                Console.WriteLine($"❌ [SocketConnectionService] Invalid start-game data: roomCode={roomCode}, hostUserId={hostUserIdStr}");
                await SendAckResponse(socketId, "start-game", false, "Invalid room code or host user ID");
                return;
            }
            
            Console.WriteLine($"🔍 [SocketConnectionService] Processing start-game for room: {roomCode}, host: {hostUserId}");
            
            if (_roomManagementService != null)
            {
                var room = await _roomManagementService.GetRoomAsync(roomCode);
                if (room == null)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Room {roomCode} not found");
                    await SendAckResponse(socketId, "start-game", false, "Room not found");
                    return;
                }
                
                var hostPlayer = room.Players.FirstOrDefault(p => p.UserId == hostUserId && p.IsHost);
                if (hostPlayer == null)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] User {hostUserId} is not host of room {roomCode}");
                    await SendAckResponse(socketId, "start-game", false, "Unauthorized: Only host can start game");
                    return;
                }
                
                if (room.Players.Count < 1)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Not enough players in room {roomCode}");
                    await SendAckResponse(socketId, "start-game", false, "Need at least 1 player to start game");
                    return;
                }
                
                Console.WriteLine($"✅ [SocketConnectionService] Room validation passed. Players in room: {room.Players.Count}");
                
                // ✅ GỬI ACK THÀNH CÔNG CHO HOST TRƯỚC
                await SendAckResponse(socketId, "start-game", true, "Game starting...");
                
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
                
                // Broadcast game-started event
                await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "game-started", gameStartData);
                
                Console.WriteLine($"📡 [SocketConnectionService] game-started broadcasted to room {roomCode}");
                Console.WriteLine($"📡 [SocketConnectionService] Players notified: {string.Join(",", room.Players.Select(p => p.Username))}");
                
                // Cập nhật game state ở backend
                room.GameState = "starting";
            }
            else
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService not available");
                await SendAckResponse(socketId, "start-game", false, "Service not available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling start-game: {ex.Message}");
            await SendAckResponse(socketId, "start-game", false, "Internal server error");
        }
    }

    /// <summary>
    /// Xử lý player ready event
    /// </summary>
    private async Task HandlePlayerReadyEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🎯 [SocketConnectionService] player-ready received from {socketId}");

            var roomCode = data.GetValueOrDefault("roomCode")?.ToString();
            var userIdStr = data.GetValueOrDefault("userId")?.ToString();

            // Nếu không có roomCode, lấy từ mapping
            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }

            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(userIdStr))
            {
                await SendAckResponse(socketId, "player-ready", false, "Missing roomCode or userId");
                return;
            }

            if (_roomManagementService != null && int.TryParse(userIdStr, out var userId))
            {
                var room = await _roomManagementService.GetRoomAsync(roomCode);
                if (room != null)
                {
                    var player = room.Players.FirstOrDefault(p => p.UserId == userId);
                    if (player != null)
                    {
                        player.Status = "ready";

                        // Broadcast player ready status
                        await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "player-status-updated", new
                        {
                            userId = userId,
                            username = player.Username,
                            status = "ready",
                            roomCode = roomCode
                        });

                        await SendAckResponse(socketId, "player-ready", true, "Player marked as ready");
                        Console.WriteLine($"✅ [SocketConnectionService] Player {userId} marked as ready in room {roomCode}");
                    }
                    else
                    {
                        await SendAckResponse(socketId, "player-ready", false, "Player not found in room");
                    }
                }
                else
                {
                    await SendAckResponse(socketId, "player-ready", false, "Room not found");
                }
            }
            else
            {
                await SendAckResponse(socketId, "player-ready", false, "Invalid data or service unavailable");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling player-ready: {ex.Message}");
            await SendAckResponse(socketId, "player-ready", false, "Internal server error");
        }
    }

    /// <summary>
    /// Gửi ACK response về cho client
    /// </summary>
    private async Task SendAckResponse(string socketId, string eventType, bool success, string message)
    {
        try
        {
            var response = new
            {
                type = success ? "ack" : "error",
                @event = eventType,
                data = new { 
                    success = success,
                    message = message 
                },
                timestamp = DateTime.UtcNow
            };
            
            var responseJson = JsonSerializer.Serialize(response);
            var responseBuffer = Encoding.UTF8.GetBytes(responseJson);
            
            if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(responseBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"📤 [SocketConnectionService] Sent {(success ? "ACK" : "ERROR")} for {eventType} to {socketId}: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error sending ACK response to {socketId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi message đến một socket cụ thể
    /// </summary>
    private async Task SendMessageToSocketAsync(string socketId, object message)
    {
        try
        {
            if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
            {
                var messageJson = JsonSerializer.Serialize(message);
                var messageBuffer = Encoding.UTF8.GetBytes(messageJson);
                
                await socket.SendAsync(messageBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"📤 [SocketConnectionService] Sent message to {socketId}");
            }
            else
            {
                Console.WriteLine($"⚠️ [SocketConnectionService] Socket {socketId} not found or not open");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error sending message to {socketId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Connection metadata để tracking
    /// </summary>
    public class ConnectionMetadata
    {
        public string SocketId { get; set; } = "";
        public string ClientIP { get; set; } = "";
        public DateTime ConnectedAt { get; set; }
        public string Path { get; set; } = "";
        public string? RoomCodeFromPath { get; set; }
        public string UserAgent { get; set; } = "";
    }
}
