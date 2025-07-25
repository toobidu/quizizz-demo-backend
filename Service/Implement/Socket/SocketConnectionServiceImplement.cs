using ConsoleApp1.Service.Interface;
using ConsoleApp1.Service.Interface.Socket;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleApp1.Config;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using System.Threading;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service quản lý kết nối WebSocket - Chịu trách nhiệm:
/// 1. Khởi động/dừng WebSocket server
/// 2. Xử lý các kết nối WebSocket mới
/// 3. Quản lý danh sách các kết nối đang hoạt động
/// 4. Xử lý ping/pong để giữ kết nối sống
/// 5. Broadcasting messages tới rooms/users/sockets
/// </summary>
public class SocketConnectionServiceImplement : ConsoleApp1.Service.Interface.Socket.ISocketConnectionService, ConsoleApp1.Service.Interface.ISocketConnectionService
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ConcurrentDictionary<string, string> _socketToRoom;
    private readonly ConcurrentDictionary<string, int> _socketToUserId;
    private readonly ConcurrentDictionary<string, DateTime> _lastPongReceived;
    private HttpListener? _listener;
    private IRoomManagementSocketService? _roomManagementService;
    private IGameFlowSocketService? _gameFlowService;
    private readonly ISocketConnectionRepository? _socketConnectionRepository;
    private const int PONG_TIMEOUT_SECONDS = 180;
    private const int PING_INTERVAL_SECONDS = 45;
    private const int MAX_MISSED_PONGS = 3;
    private readonly ConcurrentDictionary<string, int> _missedPongCounts = new();
    private readonly List<Timer> _activeTimers = new();

    public SocketConnectionServiceImplement(
        ConcurrentDictionary<string, WebSocket> connections,
        ConcurrentDictionary<string, string> socketToRoom,
        ISocketConnectionRepository? socketConnectionRepository = null)
    {
        _connections = connections;
        _socketToRoom = socketToRoom;
        _socketToUserId = new ConcurrentDictionary<string, int>();
        _lastPongReceived = new ConcurrentDictionary<string, DateTime>();
        _socketConnectionRepository = socketConnectionRepository;
    }

    public SocketConnectionServiceImplement()
    {
        _connections = new ConcurrentDictionary<string, WebSocket>();
        _socketToRoom = new ConcurrentDictionary<string, string>();
        _socketToUserId = new ConcurrentDictionary<string, int>();
        _lastPongReceived = new ConcurrentDictionary<string, DateTime>();
        _socketConnectionRepository = null;
    }

    public void SetRoomManagementService(IRoomManagementSocketService roomManagementService)
    {
        _roomManagementService = roomManagementService;
    }

    public void SetGameFlowService(IGameFlowSocketService gameFlowService)
    {
        _gameFlowService = gameFlowService;
    }

    public async Task StartAsync(int port)
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{port}/");
        _listener.Start();
        
        Console.WriteLine($"🔌 [SocketConnectionService] WebSocket server started on port {port}");

        _ = Task.Run(async () =>
        {
            while (_listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleWebSocketRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    Console.WriteLine("🛑 [SocketConnectionService] Server stopped listening");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Error in listener loop: {ex.Message}");
                }
            }
        });

        await Task.Delay(100);
    }

    public async Task StopAsync()
    {
        Console.WriteLine("🛑 [SocketConnectionService] Stopping WebSocket server...");
        
        foreach (var timer in _activeTimers.ToList())
        {
            timer?.Dispose();
        }
        _activeTimers.Clear();
        
        var closeTasks = new List<Task>();
        foreach (var kvp in _connections.ToList())
        {
            var socketId = kvp.Key;
            var connection = kvp.Value;
            
            if (connection.State == WebSocketState.Open)
            {
                closeTasks.Add(CloseConnectionGracefully(socketId, connection, "Server shutdown"));
            }
        }
        
        try
        {
            await Task.WhenAll(closeTasks).WaitAsync(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⚠️ [SocketConnectionService] Some connections failed to close within timeout");
        }
        
        _connections.Clear();
        _socketToRoom.Clear();
        _socketToUserId.Clear();
        _lastPongReceived.Clear();
        _missedPongCounts.Clear();

        try
        {
            _listener?.Stop();
            _listener?.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [SocketConnectionService] Error stopping listener: {ex.Message}");
        }
        
        Console.WriteLine("✅ [SocketConnectionService] WebSocket server stopped");
    }

    private async Task CloseConnectionGracefully(string socketId, WebSocket connection, string reason)
    {
        try
        {
            Console.WriteLine($"🔌 [SocketConnectionService] Closing connection {socketId}: {reason}");
            
            if (_socketToRoom.TryGetValue(socketId, out var roomCode) && _roomManagementService != null)
            {
                await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
            }
            
            await connection.CloseAsync(
                WebSocketCloseStatus.NormalClosure, 
                reason, 
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [SocketConnectionService] Error closing connection {socketId}: {ex.Message}");
        }
        finally
        {
            _connections.TryRemove(socketId, out _);
            _socketToRoom.TryRemove(socketId, out _);
            _socketToUserId.TryRemove(socketId, out _);
            _lastPongReceived.TryRemove(socketId, out _);
            _missedPongCounts.TryRemove(socketId, out _);
        }
    }

    private bool IsValidWebSocketPath(string path)
    {
        return path == "/" ||
               path.StartsWith("/waiting-room/", StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith("/ws/waiting-room/", StringComparison.OrdinalIgnoreCase);
    }

    private string? ExtractRoomCodeFromPath(string path)
    {
        if (path.StartsWith("/waiting-room/", StringComparison.OrdinalIgnoreCase))
        {
            var roomCode = path.Substring("/waiting-room/".Length);
            if (!string.IsNullOrEmpty(roomCode) && IsValidRoomCode(roomCode))
            {
                return roomCode;
            }
        }
        else if (path.StartsWith("/ws/waiting-room/", StringComparison.OrdinalIgnoreCase))
        {
            var roomCode = path.Substring("/ws/waiting-room/".Length);
            if (!string.IsNullOrEmpty(roomCode) && IsValidRoomCode(roomCode))
            {
                return roomCode;
            }
        }
        return null;
    }

    private async Task HandleWebSocketRequestAsync(HttpListenerContext context)
    {
        var clientIP = context.Request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        var path = context.Request.Url?.AbsolutePath ?? "/";
        var userAgent = context.Request.Headers["User-Agent"] ?? "unknown";

        Console.WriteLine($"📡 [WebSocket] New request from {clientIP} to path: {path}");
        Console.WriteLine($"📡 [WebSocket] User-Agent: {userAgent}");

        try
        {
            if (!context.Request.IsWebSocketRequest)
            {
                Console.WriteLine($"❌ [WebSocket] Not a WebSocket request from {clientIP}");
                context.Response.StatusCode = 400;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("WebSocket connection required"));
                context.Response.Close();
                return;
            }

            if (!IsValidWebSocketPath(path))
            {
                Console.WriteLine($"❌ [WebSocket] Invalid path '{path}' from {clientIP}");
                context.Response.StatusCode = 404;
                await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes($"Path '{path}' not supported"));
                context.Response.Close();
                return;
            }

            var roomCodeFromPath = ExtractRoomCodeFromPath(path);
            if (!string.IsNullOrEmpty(roomCodeFromPath))
            {
                Console.WriteLine($"🏠 [WebSocket] Room code from path: {roomCodeFromPath}");
            }

            var webSocketContext = await context.AcceptWebSocketAsync(null);
            var webSocket = webSocketContext.WebSocket;
            var socketId = Guid.NewGuid().ToString();

            Console.WriteLine($"🔗 [WebSocket] Connection accepted: {socketId} from {clientIP}");
            Console.WriteLine($"🔌 [WebSocket] Connection state: {webSocket.State}");

            var metadata = new ConnectionMetadata
            {
                SocketId = socketId,
                ClientIP = clientIP,
                ConnectedAt = DateTime.UtcNow,
                Path = path,
                RoomCodeFromPath = roomCodeFromPath,
                UserAgent = userAgent
            };

            if (!_connections.TryAdd(socketId, webSocket))
            {
                Console.WriteLine($"⚠️ [WebSocket] Failed to add socket {socketId} to connections");
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Failed to add connection", CancellationToken.None);
                return;
            }

            var buffer = new byte[1024 * 4];
            int messageCount = 0;
            int pingCount = 0;

            var cts = new CancellationTokenSource();
            var pingTimer = new Timer(async state =>
            {
                try
                {
                    if (cts.Token.IsCancellationRequested)
                        return;

                    pingCount++;
                    var lastPongReceived = _lastPongReceived.GetOrAdd(socketId, DateTime.UtcNow);
                    var timeSinceLastPong = DateTime.UtcNow - lastPongReceived;

                    if (timeSinceLastPong.TotalSeconds > PING_INTERVAL_SECONDS * 2)
                    {
                        var missedCount = _missedPongCounts.GetOrAdd(socketId, 0) + 1;
                        _missedPongCounts[socketId] = missedCount;

                        if (missedCount >= MAX_MISSED_PONGS)
                        {
                            Console.WriteLine($"⏰ [WebSocket] Too many missed pongs for {socketId} ({missedCount}/{MAX_MISSED_PONGS})");
                            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Pong timeout", CancellationToken.None);
                            cts.Cancel();
                            return;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ [WebSocket] Missed pong {missedCount}/{MAX_MISSED_PONGS} for {socketId} (last: {timeSinceLastPong.TotalSeconds:F1}s ago)");
                        }
                    }
                    else
                    {
                        _missedPongCounts.TryRemove(socketId, out _);
                    }

                    var pingMessage = JsonSerializer.Serialize(new
                    {
                        type = "ping",
                        data = new
                        {
                            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            pingCount = pingCount
                        },
                        timestamp = DateTime.UtcNow
                    });

                    var pingBuffer = Encoding.UTF8.GetBytes(pingMessage);
                    await webSocket.SendAsync(new ArraySegment<byte>(pingBuffer), WebSocketMessageType.Text, true, cts.Token);
                    Console.WriteLine($"🏓 [WebSocket] Sent ping #{pingCount} to {socketId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [WebSocket] Error sending ping to {socketId}: {ex.Message}");
                    cts.Cancel();
                }
            }, null, TimeSpan.FromSeconds(PING_INTERVAL_SECONDS), TimeSpan.FromSeconds(PING_INTERVAL_SECONDS));

            _activeTimers.Add(pingTimer);

            try
            {
                while (webSocket.State == WebSocketState.Open)
                {
                    Console.WriteLine($"👂 [WebSocket] Waiting for message from {socketId}...");
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    Console.WriteLine($"📨 [WebSocket] Received from {socketId}: Type={result.MessageType}, Count={result.Count}, EndOfMessage={result.EndOfMessage}");

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        messageCount++;
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
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
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"⏹️ [WebSocket] Operation cancelled for {socketId}");
            }
            catch (WebSocketException wsEx)
            {
                Console.WriteLine($"🔌 [WebSocket] WebSocket exception for {socketId}: {wsEx.Message}");
                Console.WriteLine($"🔌 [WebSocket] WebSocket error code: {wsEx.WebSocketErrorCode}");
                Console.WriteLine($"🔌 [WebSocket] Native error code: {wsEx.NativeErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [WebSocket] Unexpected error for {socketId}: {ex.Message}");
                Console.WriteLine($"❌ [WebSocket] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"❌ [WebSocket] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                cts.Cancel();
                pingTimer?.Dispose();
                _activeTimers.Remove(pingTimer);
                var duration = DateTime.UtcNow - metadata.ConnectedAt;
                Console.WriteLine($"🔌 [WebSocket] Connection {socketId} closing after {duration.TotalMinutes:F1} minutes");
                Console.WriteLine($"📊 [WebSocket] Stats for {socketId}: {messageCount} messages, {pingCount} pings");
                Console.WriteLine($"🔌 [WebSocket] Final state: {webSocket.State}");

                _connections.TryRemove(socketId, out _);

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

                        try
                        {
                            if (_socketConnectionRepository != null)
                            {
                                await _socketConnectionRepository.DeleteBySocketIdAsync(socketId);
                                Console.WriteLine($"🗑️ [WebSocket] Deleted SocketConnection for {socketId} from database");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ [WebSocket] Error deleting SocketConnection for {socketId}: {ex.Message}");
                        }
                    }
                }

                Console.WriteLine($"✅ [WebSocket] Cleanup completed for {socketId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [WebSocket] Error handling WebSocket request from {clientIP}: {ex.Message}");
        }
    }

    private async Task ProcessWebSocketMessage(string socketId, string message)
    {
        var startTime = DateTime.UtcNow;

        try
        {
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

            var eventName = ExtractStringValue(data, "event") ?? ExtractStringValue(data, "type");
            if (string.IsNullOrEmpty(eventName))
            {
                Console.WriteLine($"⚠️ [Message] Missing event name from {socketId}");
                await SendAckResponse(socketId, "unknown", false, "Missing event name");
                return;
            }

            Console.WriteLine($"🎯 [Message] Processing event '{eventName}' from {socketId}");

            await UpdateLastActivity(socketId);

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
                    _lastPongReceived[socketId] = DateTime.UtcNow;
                    _missedPongCounts.TryRemove(socketId, out _);
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
            await SendAckResponse(socketId, "unknown", false, "Internal server error");
        }
    }

    private async Task ValidateAndHandleJoinRoom(string socketId, Dictionary<string, object> data, string eventName)
    {
        Console.WriteLine($"🚪 [JoinRoom] Processing join-room from {socketId}");
        
        var roomCode = ExtractStringValue(data, "roomCode");
        var username = ExtractStringValue(data, "username"); 
        var userIdStr = ExtractStringValue(data, "userId");
        
        Console.WriteLine($"🚪 [JoinRoom] Data - roomCode: {roomCode}, username: {username}, userId: {userIdStr}");
        
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
        
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
        {
            Console.WriteLine($"❌ [JoinRoom] Invalid userId '{userIdStr}' from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Invalid user ID");
            return;
        }
        
        if (!IsValidRoomCode(roomCode))
        {
            Console.WriteLine($"❌ [JoinRoom] Invalid roomCode format '{roomCode}' from {socketId}");
            await SendAckResponse(socketId, eventName, false, "Invalid room code format");
            return;
        }
        
        Console.WriteLine($"✅ [JoinRoom] Validation passed for {username} joining {roomCode}");
        
        await HandleJoinRoomEvent(socketId, data);
    }

    private bool IsValidRoomCode(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode)) return false;
        if (roomCode.Length < RoomManagementConstants.Limits.MinRoomCodeLength) return false;
        if (roomCode.Length > RoomManagementConstants.Limits.MaxRoomCodeLength) return false;
        
        return roomCode.All(char.IsLetterOrDigit);
    }

    private string? ExtractStringValue(Dictionary<string, object> data, string key)
    {
        var directValue = data.GetValueOrDefault(key)?.ToString();
        if (!string.IsNullOrEmpty(directValue)) 
        {
            Console.WriteLine($"🔍 [ExtractStringValue] Found '{key}' direct: '{directValue}'");
            return directValue;
        }
        
        if (data.ContainsKey("data"))
        {
            try
            {
                var dataValueStr = data["data"].ToString();
                Console.WriteLine($"🔍 [ExtractStringValue] Checking nested data: {dataValueStr}");
                
                if (!string.IsNullOrEmpty(dataValueStr))
                {
                    var nestedData = JsonSerializer.Deserialize<Dictionary<string, object>>(dataValueStr);
                    if (nestedData != null)
                    {
                        var nestedValue = nestedData.GetValueOrDefault(key)?.ToString();
                        if (!string.IsNullOrEmpty(nestedValue))
                        {
                            Console.WriteLine($"🔍 [ExtractStringValue] Found '{key}' nested: '{nestedValue}'");
                            return nestedValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [ExtractStringValue] Error parsing nested data for '{key}': {ex.Message}");
            }
        }
        
        Console.WriteLine($"⚠️ [ExtractStringValue] Key '{key}' not found in data");
        return null;
    }

    private async Task HandleJoinRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] join-room received from {socketId}");
            
            var roomCode = ExtractStringValue(data, "roomCode");
            var username = ExtractStringValue(data, "username");
            var userIdStr = ExtractStringValue(data, "userId");

            Console.WriteLine($"🚪 [HandleJoinRoom] Extracted - roomCode: '{roomCode}', username: '{username}', userId: '{userIdStr}'");

            if (string.IsNullOrEmpty(roomCode))
            {
                Console.WriteLine($"❌ [HandleJoinRoom] Missing roomCode from {socketId}");
                await SendAckResponse(socketId, "join-room", false, "Missing room code");
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                Console.WriteLine($"❌ [HandleJoinRoom] Missing username from {socketId}");
                await SendAckResponse(socketId, "join-room", false, "Missing username");
                return;
            }

            if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
            {
                Console.WriteLine($"❌ [HandleJoinRoom] Invalid userId '{userIdStr}' from {socketId}");
                await SendAckResponse(socketId, "join-room", false, "Invalid user ID");
                return;
            }

            _socketToRoom[socketId] = roomCode;
            _socketToUserId[socketId] = userId;

            Console.WriteLine($"✅ [HandleJoinRoom] Validation passed. Proceeding with room join for {username} (ID: {userId}) to room {roomCode}");

            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "join-room", false, "Room service not available");
                return;
            }

            Console.WriteLine($"🎮 [HandleJoinRoom] Calling RoomManagementService.JoinRoomAsync...");
            await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
            
            Console.WriteLine($"💾 [HandleJoinRoom] Creating socket connection in database...");
            await CreateSocketConnectionInDatabase(socketId, userId, roomCode);
            
            await SendAckResponse(socketId, "join-room", true, "Successfully joined room");
            
            Console.WriteLine($"✅ [SocketConnectionService] {username} joined room {roomCode}");
            
            try
            {
                var room = await _roomManagementService.GetRoomAsync(roomCode);
                if (room != null)
                {
                    await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "player-joined", new
                    {
                        userId = userId,
                        username = username,
                        isHost = room.Players.Any(p => p.UserId == userId && p.IsHost),
                        roomCode = roomCode,
                        timestamp = DateTime.UtcNow
                    });
                    
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling join-room: {ex.Message}");
            await SendAckResponse(socketId, "join-room", false, "Internal server error");
        }
    }

    private async Task HandleLeaveRoomEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] leave-room received from {socketId}");
            
            string? roomCode = ExtractStringValue(data, "roomCode");
            string? userIdStr = ExtractStringValue(data, "userId");

            if (string.IsNullOrEmpty(roomCode))
            {
                if (!_socketToRoom.TryGetValue(socketId, out roomCode))
                {
                    await SendAckResponse(socketId, "leave-room", false, "Room code not found");
                    return;
                }
            }

            if (!_socketToRoom.TryGetValue(socketId, out var currentRoom) || currentRoom != roomCode)
            {
                await SendAckResponse(socketId, "leave-room", false, "Not in specified room");
                return;
            }

            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "leave-room", false, "Room service not available");
                return;
            }

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                await _roomManagementService.LeaveRoomByUserIdAsync(userId, roomCode);
            }
            else
            {
                await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
            }

            _socketToRoom.TryRemove(socketId, out _);
            _socketToUserId.TryRemove(socketId, out _);

            await SendAckResponse(socketId, "leave-room", true, "Successfully left room");
            
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling leave-room: {ex.Message}");
            await SendAckResponse(socketId, "leave-room", false, "Internal server error");
        }
    }

    private async Task HandleRequestPlayersUpdateEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🔄 [SocketConnectionService] request-players-update received from {socketId}");

            string? roomCode = ExtractStringValue(data, "roomCode");

            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }

            if (string.IsNullOrEmpty(roomCode))
            {
                await SendAckResponse(socketId, "request-players-update", false, "Room code not found");
                return;
            }

            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "request-players-update", false, "Room service not available");
                return;
            }

            await _roomManagementService.RequestPlayersUpdateAsync(socketId, roomCode);
            await SendAckResponse(socketId, "request-players-update", true, "Players update sent");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling request-players-update: {ex.Message}");
            await SendAckResponse(socketId, "request-players-update", false, "Internal server error");
        }
    }

    private async Task HandlePingEvent(string socketId)
    {
        try
        {
            Console.WriteLine($"🏓 [SocketConnectionService] Ping received from {socketId}");
            
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

    private async Task HandleHeartbeatEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"💓 [SocketConnectionService] Heartbeat received from {socketId}");
            
            _lastPongReceived[socketId] = DateTime.UtcNow;
            
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

    private async Task HandlePlayerLeftEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🚪 [SocketConnectionService] player-left received from {socketId}");
            
            var roomCode = ExtractStringValue(data, "roomCode");
            var userIdStr = ExtractStringValue(data, "userId");
            var username = ExtractStringValue(data, "username");
            
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
            
            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "player-left", false, "Room service not available");
                return;
            }

            if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var userId))
            {
                await _roomManagementService.LeaveRoomByUserIdAsync(userId, roomCode);
                
                await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, "player-left", new
                {
                    userId = userId,
                    username = username ?? "Unknown",
                    roomCode = roomCode,
                    timestamp = DateTime.UtcNow
                });
                
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
            
            _socketToRoom.TryRemove(socketId, out _);
            _socketToUserId.TryRemove(socketId, out _);
            
            await SendAckResponse(socketId, "player-left", true, "Player left successfully");
            Console.WriteLine($"✅ [SocketConnectionService] Player left room {roomCode} successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling player-left: {ex.Message}");
            await SendAckResponse(socketId, "player-left", false, "Internal server error");
        }
    }

    private async Task HandleStartGameEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🎮 [SocketConnectionService] start-game received from {socketId}");
            Console.WriteLine($"🎮 [SocketConnectionService] Request data: {JsonSerializer.Serialize(data)}");

            string? roomCode = ExtractStringValue(data, "roomCode");
            string? hostUserIdStr = ExtractStringValue(data, "hostUserId") ?? ExtractStringValue(data, "userId");

            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
                Console.WriteLine($"🔍 [SocketConnectionService] Got roomCode from mapping: {roomCode}");
            }

            if (string.IsNullOrEmpty(roomCode) || !int.TryParse(hostUserIdStr, out var hostUserId))
            {
                Console.WriteLine($"❌ [SocketConnectionService] Invalid start-game data: roomCode='{roomCode}', hostUserId='{hostUserIdStr}'");
                await SendAckResponse(socketId, "start-game", false, "Invalid room code or host user ID");
                return;
            }

            Console.WriteLine($"🔍 [SocketConnectionService] Processing start-game for room: {roomCode}, host: {hostUserId}");

            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "start-game", false, "Room service not available");
                return;
            }

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

            await SendAckResponse(socketId, "start-game", true, "Game starting...");

            if (_gameFlowService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] GameFlowService is not initialized");
                await SendAckResponse(socketId, "start-game", false, "Game service not available");
                return;
            }

            await _gameFlowService.StartGameAsync(roomCode);
            Console.WriteLine($"🎮 [SocketConnectionService] Started game flow for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling start-game: {ex.Message}");
            await SendAckResponse(socketId, "start-game", false, "Internal server error");
        }
    }

    private async Task HandlePlayerReadyEvent(string socketId, Dictionary<string, object> data)
    {
        try
        {
            Console.WriteLine($"🎯 [SocketConnectionService] player-ready received from {socketId}");

            var roomCode = ExtractStringValue(data, "roomCode");
            var userIdStr = ExtractStringValue(data, "userId");

            if (string.IsNullOrEmpty(roomCode))
            {
                _socketToRoom.TryGetValue(socketId, out roomCode);
            }

            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(userIdStr))
            {
                await SendAckResponse(socketId, "player-ready", false, "Missing roomCode or userId");
                return;
            }

            if (_roomManagementService == null)
            {
                Console.WriteLine($"❌ [SocketConnectionService] RoomManagementService is not initialized");
                await SendAckResponse(socketId, "player-ready", false, "Room service not available");
                return;
            }

            if (!int.TryParse(userIdStr, out var userId))
            {
                await SendAckResponse(socketId, "player-ready", false, "Invalid userId");
                return;
            }

            var room = await _roomManagementService.GetRoomAsync(roomCode);
            if (room != null)
            {
                var player = room.Players.FirstOrDefault(p => p.UserId == userId);
                if (player != null)
                {
                    player.Status = "ready";

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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error handling player-ready: {ex.Message}");
            await SendAckResponse(socketId, "player-ready", false, "Internal server error");
        }
    }

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
            
            await SendMessageToSocketAsync(socketId, response);
            Console.WriteLine($"📤 [SocketConnectionService] Sent {(success ? "ACK" : "ERROR")} for {eventType} to {socketId}: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error sending ACK response to {socketId}: {ex.Message}");
        }
    }

    private async Task SendMessageToSocketAsync(string socketId, object message)
    {
        try
        {
            if (!_connections.TryGetValue(socketId, out var socket))
            {
                Console.WriteLine($"⚠️ [SocketConnectionService] Socket {socketId} not found in _connections");
                return;
            }

            if (socket.State != WebSocketState.Open)
            {
                Console.WriteLine($"⚠️ [SocketConnectionService] Socket {socketId} is not open (state: {socket.State})");
                return;
            }

            var messageJson = JsonSerializer.Serialize(message);
            var messageBuffer = Encoding.UTF8.GetBytes(messageJson);
            await socket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"📤 [SocketConnectionService] Sent message to {socketId}: event={message.GetType().GetProperty("event")?.GetValue(message)?.ToString() ?? "unknown"}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error sending message to {socketId}: {ex.Message}");
        }
    }

    public async Task BroadcastToRoomAsync(string roomCode, string eventType, object data)
    {
        try
        {
            var socketIds = _socketToRoom
                .Where(kv => kv.Value == roomCode)
                .Select(kv => kv.Key)
                .ToList();

            Console.WriteLine($"📡 [SocketConnectionService] Broadcasting '{eventType}' to room '{roomCode}'. Found {socketIds.Count} socket IDs: [{string.Join(", ", socketIds)}]");
            if (!socketIds.Any())
            {
                Console.WriteLine($"⚠️ [SocketConnectionService] No active connections found for room '{roomCode}'");
                return;
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var socketId in socketIds)
            {
                try
                {
                    await SendMessageToSocketAsync(socketId, new
                    {
                        @event = eventType,
                        data = data,
                        timestamp = DateTime.UtcNow
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    Console.WriteLine($"❌ [SocketConnectionService] Failed to send '{eventType}' to socket '{socketId}': {ex.Message}");
                }
            }

            Console.WriteLine($"📤 [SocketConnectionService] Broadcast '{eventType}' to room '{roomCode}' completed: {successCount} success, {failCount} failed out of {socketIds.Count} total");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in BroadcastToRoomAsync for room '{roomCode}', event '{eventType}': {ex.Message}");
        }
    }

    public async Task BroadcastToUserAsync(int userId, string eventType, object data)
    {
        try
        {
            var socketIds = _socketToUserId
                .Where(kv => kv.Value == userId)
                .Select(kv => kv.Key)
                .ToList();

            Console.WriteLine($"📡 [SocketConnectionService] Broadcasting '{eventType}' to user {userId}. Found {socketIds.Count} socket connections");

            foreach (var socketId in socketIds)
            {
                try
                {
                    await SendMessageToSocketAsync(socketId, new
                    {
                        @event = eventType,
                        data = data,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Failed to send '{eventType}' to user {userId} socket '{socketId}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in BroadcastToUserAsync for user {userId}, event '{eventType}': {ex.Message}");
        }
    }

    public async Task BroadcastToSocketAsync(string socketId, string eventType, object data)
    {
        try
        {
            await SendMessageToSocketAsync(socketId, new
            {
                @event = eventType,
                data = data,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in BroadcastToSocketAsync for socket '{socketId}', event '{eventType}': {ex.Message}");
        }
    }

    public async Task BroadcastToAllAsync(string eventType, object data)
    {
        try
        {
            var socketIds = _connections.Keys.ToList();
            Console.WriteLine($"📡 [SocketConnectionService] Broadcasting '{eventType}' to all connections. Found {socketIds.Count} active connections");

            foreach (var socketId in socketIds)
            {
                try
                {
                    await SendMessageToSocketAsync(socketId, new
                    {
                        @event = eventType,
                        data = data,
                        timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [SocketConnectionService] Failed to send '{eventType}' to socket '{socketId}': {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error in BroadcastToAllAsync for event '{eventType}': {ex.Message}");
        }
    }

    private async Task BroadcastGameStarted(string roomCode, object gameData)
    {
        try
        {
            Console.WriteLine($"📡 [SocketConnectionService] Broadcasting game-started for room '{roomCode}'");
            await BroadcastToRoomAsync(roomCode, "game-started", gameData);
            Console.WriteLine($"✅ [SocketConnectionService] Successfully broadcasted game-started for room '{roomCode}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Failed to broadcast game-started for room '{roomCode}': {ex.Message}");
        }
    }

    private async Task CreateSocketConnectionInDatabase(string socketId, int userId, string roomCode)
    {
        try
        {
            Console.WriteLine($"💾 [SocketConnectionService] Creating SocketConnection in database: socketId={socketId}, userId={userId}, roomCode={roomCode}");
            
            if (_socketConnectionRepository == null)
            {
                Console.WriteLine($"⚠️ [SocketConnectionService] SocketConnectionRepository not available, skipping database save");
                return;
            }
            
            var now = DateTime.UtcNow;
            var connection = new SocketConnection
            {
                SocketId = socketId,
                UserId = userId,
                RoomId = null,
                ConnectedAt = now,
                LastActivity = now
            };

            var connectionId = await _socketConnectionRepository.CreateAsync(connection);
            if (connectionId > 0)
            {
                Console.WriteLine($"✅ [SocketConnectionService] Created SocketConnection with ID: {connectionId}");
                
                var updated = await _socketConnectionRepository.UpdateRoomIdAsync(socketId, roomCode);
                if (updated)
                {
                    Console.WriteLine($"✅ [SocketConnectionService] Updated room_id for socket {socketId} to room {roomCode}");
                }
                else
                {
                    Console.WriteLine($"⚠️ [SocketConnectionService] Failed to update room_id for socket {socketId}");
                }
            }
            else
            {
                Console.WriteLine($"❌ [SocketConnectionService] Failed to create SocketConnection for socket {socketId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error creating SocketConnection: {ex.Message}");
            Console.WriteLine($"❌ [SocketConnectionService] Stack trace: {ex.StackTrace}");
        }
    }

    private async Task UpdateLastActivity(string socketId)
    {
        try
        {
            if (_socketConnectionRepository != null)
            {
                await _socketConnectionRepository.UpdateLastActivityAsync(socketId);
                Console.WriteLine($"🔄 [SocketConnectionService] Updated last activity for socket {socketId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionService] Error updating last activity for {socketId}: {ex.Message}");
        }
    }

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
