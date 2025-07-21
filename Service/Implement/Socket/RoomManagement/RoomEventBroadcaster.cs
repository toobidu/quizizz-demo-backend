using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;

/// <summary>
/// Service broadcast events cho Room Management
/// </summary>
public class RoomEventBroadcaster
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();

    public RoomEventBroadcaster(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
    }

    /// <summary>
    /// Broadcast room players update
    /// </summary>
    public async Task BroadcastRoomPlayersUpdateAsync(string roomCode, List<GamePlayer> players)
    {
        // Đảm bảo định dạng dữ liệu đúng với mong đợi của frontend
        var eventData = new
        {
            roomCode = roomCode,  // Chuyển thành camelCase để phù hợp với frontend
            players = players.Select(p => new {
                userId = p.UserId,
                username = p.Username,
                score = p.Score,
                isHost = p.IsHost,
                status = p.Status,
                timeTaken = "00:00:00" // Thêm trường TimeTaken để đảm bảo định dạng nhất quán
            }).ToList(),
            totalPlayers = players.Count,
            maxPlayers = 10, // Giá trị mặc định vì GameRoom không có thuộc tính MaxPlayers
            status = "waiting", // Giá trị mặc định vì GameRoom không có thuộc tính Status
            host = players.FirstOrDefault(p => p.IsHost)?.Username
        };

        // Chỉ gửi một lần duy nhất cho mỗi phòng
        await BroadcastToRoomAsync(roomCode, "room-players-updated", eventData, true);
        Console.WriteLine($"[ROOM] Updated player list for room {roomCode}: {players.Count} players");
        
        // Log chi tiết để debug
        Console.WriteLine($"[ROOM] room-players-updated event data: {JsonSerializer.Serialize(eventData)}");
    }

    /// <summary>
    /// Broadcast room players update with player-joined event
    /// Gửi cả hai events: PlayerJoined và RoomPlayersUpdated
    /// </summary>
    public async Task BroadcastRoomPlayersUpdateWithPlayerJoinedAsync(string roomCode, List<GamePlayer> players, GamePlayer? newPlayer = null)
    {
        // Nếu có player mới, gửi event PlayerJoined trước
        if (newPlayer != null)
        {
            var playerJoinedData = new
            {
                userId = newPlayer.UserId,
                username = newPlayer.Username,
                score = newPlayer.Score,
                timeTaken = "00:00:00" // Player mới luôn có time = 0
            };

            await BroadcastToRoomAsync(roomCode, "player-joined", playerJoinedData);
            Console.WriteLine($"[ROOM] Broadcasted PlayerJoined event for {newPlayer.Username} in room {roomCode}");
        }

        // Sau đó gửi event RoomPlayersUpdated
        await BroadcastRoomPlayersUpdateAsync(roomCode, players);
    }
    
    /// <summary>
    /// Broadcast room players update with player-left event
    /// Gửi cả hai events: PlayerLeft và RoomPlayersUpdated
    /// </summary>
    public async Task BroadcastRoomPlayersUpdateWithPlayerLeftAsync(string roomCode, List<GamePlayer> players, GamePlayer leftPlayer)
    {
        // Gửi event PlayerLeft trước
        var playerLeftData = new
        {
            userId = leftPlayer.UserId,
            username = leftPlayer.Username
        };

        await BroadcastToRoomAsync(roomCode, "player-left", playerLeftData);
        Console.WriteLine($"[ROOM] Broadcasted PlayerLeft event for {leftPlayer.Username} in room {roomCode}");

        // Đợi một chút để đảm bảo client xử lý sự kiện player-left trước
        await Task.Delay(200);
        
        // Sau đó gửi event RoomPlayersUpdated
        await BroadcastRoomPlayersUpdateAsync(roomCode, players);
        Console.WriteLine($"[ROOM] Broadcasted RoomPlayersUpdated after player {leftPlayer.Username} left room {roomCode}");
    }

    /// <summary>
    /// Broadcast host change event
    /// </summary>
    public async Task BroadcastHostChangeAsync(string roomCode, GamePlayer newHost)
    {
        var eventData = new
        {
            newHost = newHost.Username,
            newHostId = newHost.UserId,
            message = $"{newHost.Username} đã trở thành host mới"
        };

        await BroadcastToRoomAsync(roomCode, "host-changed", eventData);
    }

    /// <summary>
    /// Send welcome message to player
    /// </summary>
    public async Task SendWelcomeMessageAsync(string socketId, string roomCode, bool isHost, string message)
    {
        var eventData = new
        {
            roomCode = roomCode,
            isHost = isHost,
            message = message
        };

        await SendToPlayerAsync(socketId, "room-joined", eventData);
    }

    /// <summary>
    /// Broadcast player-joined event to other players in room
    /// </summary>
    public async Task BroadcastPlayerJoinedEventAsync(string roomCode, object playerData)
    {
        await BroadcastToRoomAsync(roomCode, "player-joined", playerData);
        Console.WriteLine($"[ROOM] Broadcasted player-joined event for room {roomCode}");
    }

    /// <summary>
    /// Gửi message đến tất cả client trong một phòng cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="eventName">Tên sự kiện</param>
    /// <param name="data">Dữ liệu gửi đi</param>
    /// <param name="preventDuplicates">Nếu true, sẽ kiểm tra và ngăn chặn gửi trùng lặp</param>
    public async Task BroadcastToRoomAsync(string roomCode, string eventName, object data, bool preventDuplicates = false)
    {
        // Nếu cần ngăn chặn trùng lặp và là sự kiện room-players-updated
        if (preventDuplicates && eventName == "room-players-updated")
        {
            // Tạo key để theo dõi sự kiện đã gửi
            string cacheKey = $"last_update_{roomCode}";
            
            // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 1 giây)
            if (_lastUpdateTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 1000)
            {
                Console.WriteLine($"[WEBSOCKET] Skipping duplicate room-players-updated for room {roomCode} (sent {(DateTime.UtcNow - lastTime).TotalMilliseconds}ms ago)");
                return;
            }
            
            // Cập nhật thời gian gửi mới nhất
            _lastUpdateTimes[cacheKey] = DateTime.UtcNow;
        }
        
        var messageObj = new {
            type = eventName,
            data = data,
            timestamp = DateTime.UtcNow
        };
        var message = JsonSerializer.Serialize(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"[WEBSOCKET] Broadcasting to room {roomCode}: {eventName}");
        
        // Log chi tiết hơn về tin nhắn được gửi
        if (eventName == "room-players-updated" || eventName == "player-joined")
        {
            Console.WriteLine($"[WEBSOCKET] Message content: {message}");
            
            // Kiểm tra định dạng dữ liệu
            if (eventName == "room-players-updated")
            {
                try
                {
                    var dataObj = data.GetType().GetProperty("players")?.GetValue(data);
                    var count = dataObj?.GetType().GetProperty("Count")?.GetValue(dataObj);
                    Console.WriteLine($"[WEBSOCKET] room-players-updated contains {count} players");
                    
                    // Kiểm tra xem có trường totalPlayers không
                    var totalPlayers = data.GetType().GetProperty("totalPlayers")?.GetValue(data);
                    Console.WriteLine($"[WEBSOCKET] totalPlayers field value: {totalPlayers}");
                    
                    // Kiểm tra xem có trường maxPlayers không
                    var maxPlayers = data.GetType().GetProperty("maxPlayers")?.GetValue(data);
                    Console.WriteLine($"[WEBSOCKET] maxPlayers field value: {maxPlayers}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WEBSOCKET] Error checking data format: {ex.Message}");
                }
            }
        }

        // Kiểm tra xem có phòng trong _gameRooms không
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom) || gameRoom.Players.Count == 0)
        {
            Console.WriteLine($"[WEBSOCKET] Không tìm thấy phòng {roomCode} trong _gameRooms hoặc phòng trống");
            Console.WriteLine($"[WEBSOCKET] Gửi đến tất cả WebSocket connections");
            
            // Gửi đến tất cả active WebSocket connections
            var broadcastTasks = _connections.Values
                .Where(socket => socket.State == WebSocketState.Open)
                .Select(async socket =>
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROOM] Failed to send message to WebSocket connection: {ex.Message}");
                    }
                });

            await Task.WhenAll(broadcastTasks);
            Console.WriteLine($"[ROOM] Broadcasted {eventName} event to {_connections.Count} WebSocket connections");
            return;
        }

        int sentCount = 0;
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
                        Interlocked.Increment(ref sentCount);
                        Console.WriteLine($"[ROOM] Sent {eventName} to {player.Username} (ID: {player.UserId})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROOM] Failed to send message to {player.Username}: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"[ROOM] Could not send {eventName} to {player.Username} - socket not found or closed");
                }
            });

        await Task.WhenAll(sendTasks);
        Console.WriteLine($"[ROOM] Broadcasted {eventName} event to {sentCount} of {gameRoom.Players.Count} players in room {roomCode}");
    }

    /// <summary>
    /// Gửi message đến một client cụ thể
    /// </summary>
    public async Task SendToPlayerAsync(string socketId, string eventName, object data)
    {
        if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
        {
            try
            {
                var messageObj = new {
                    type = eventName,
                    data = data,
                    timestamp = DateTime.UtcNow
                };
                var message = JsonSerializer.Serialize(messageObj);
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                
                // Log chi tiết để debug
                if (eventName == "room-players-updated" || eventName == "room-joined")
                {
                    Console.WriteLine($"[ROOM] SendToPlayerAsync - {eventName} message: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ROOM] Failed to send message to socket {socketId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Broadcast message đến tất cả WebSocket connections hiện tại
    /// Dùng khi cần gửi event mà không cần dựa vào in-memory game rooms
    /// </summary>
    public async Task BroadcastToAllConnectionsAsync(string roomCode, string eventName, object data)
    {
        var messageObj = new {
            type = eventName,
            data = data,
            timestamp = DateTime.UtcNow
        };
        var message = JsonSerializer.Serialize(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"[WEBSOCKET] Broadcasting to all connections for room {roomCode}: {message}");

        // Gửi đến tất cả active WebSocket connections
        var sendTasks = _connections.Values
            .Where(socket => socket.State == WebSocketState.Open)
            .Select(async socket =>
            {
                try
                {
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ROOM] Failed to send message to WebSocket connection: {ex.Message}");
                }
            });

        await Task.WhenAll(sendTasks);
        Console.WriteLine($"[ROOM] Broadcasted {eventName} event to {_connections.Count} WebSocket connections");
    }

    /// <summary>
    /// Broadcast message đến những người khác trong phòng (loại trừ userId được chỉ định)
    /// Dùng để gửi PlayerJoined event chỉ cho những người đã có trong phòng trước đó
    /// </summary>
    public async Task BroadcastToOthersAsync(string roomCode, int excludeUserId, string eventName, object data)
    {
        var messageObj = new {
            type = eventName,
            data = data,
            timestamp = DateTime.UtcNow
        };
        var message = JsonSerializer.Serialize(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"[WEBSOCKET] Broadcasting to others in room {roomCode} (exclude userId {excludeUserId}): {message}");

        // Kiểm tra xem có phòng trong _gameRooms không
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom) || gameRoom.Players.Count == 0)
        {
            Console.WriteLine($"[WEBSOCKET] Không tìm thấy phòng {roomCode} trong _gameRooms hoặc phòng trống");
            Console.WriteLine($"[WEBSOCKET] Gửi đến tất cả WebSocket connections");
            
            // Gửi đến tất cả active WebSocket connections
            var broadcastTasks = _connections.Values
                .Where(socket => socket.State == WebSocketState.Open)
                .Select(async socket =>
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROOM] Failed to send message to WebSocket connection: {ex.Message}");
                    }
                });

            await Task.WhenAll(broadcastTasks);
            Console.WriteLine($"[ROOM] Broadcasted {eventName} event to {_connections.Count} WebSocket connections");
            return;
        }

        // Gửi đến tất cả player trong phòng NGOẠI TRỪ người có excludeUserId
        var sendTasks = gameRoom.Players
            .Where(p => p.UserId != excludeUserId && !string.IsNullOrEmpty(p.SocketId))
            .Select(async player =>
            {
                if (_connections.TryGetValue(player.SocketId!, out var socket) &&
                    socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                        Console.WriteLine($"[ROOM] Sent {eventName} to {player.Username} (ID: {player.UserId})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ROOM] Failed to send message to {player.Username}: {ex.Message}");
                    }
                }
            });

        await Task.WhenAll(sendTasks);
        Console.WriteLine($"[ROOM] Broadcasted {eventName} event to others in room {roomCode}");
    }
}