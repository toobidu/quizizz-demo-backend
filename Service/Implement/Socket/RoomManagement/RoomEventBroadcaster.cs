using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;

/// <summary>
/// Service broadcast events cho Room Management
/// </summary>
public class RoomEventBroadcaster
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;

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
        var eventData = new
        {
            RoomCode = roomCode,
            Players = players.Select(p => new {
                UserId = p.UserId,
                Username = p.Username,
                Score = p.Score,
                IsHost = p.IsHost,
                Status = p.Status
            }).ToList(),
            TotalPlayers = players.Count,
            Host = players.FirstOrDefault(p => p.IsHost)?.Username
        };

        await BroadcastToRoomAsync(roomCode, "room-players-updated", eventData);
        Console.WriteLine($"[ROOM] Updated player list for room {roomCode}: {players.Count} players");
    }

    /// <summary>
    /// Broadcast room players update with player-joined event
    /// Gửi cả hai events: player-joined và room-players-updated
    /// </summary>
    public async Task BroadcastRoomPlayersUpdateWithPlayerJoinedAsync(string roomCode, List<GamePlayer> players, GamePlayer? newPlayer = null)
    {
        // Nếu có player mới, gửi event player-joined trước
        if (newPlayer != null)
        {
            var playerJoinedData = new
            {
                UserId = newPlayer.UserId,
                Username = newPlayer.Username,
                Score = newPlayer.Score,
                TimeTaken = "00:00:00" // Player mới luôn có time = 0
            };

            await BroadcastToRoomAsync(roomCode, "player-joined", playerJoinedData);
            Console.WriteLine($"[ROOM] Broadcasted player-joined event for {newPlayer.Username} in room {roomCode}");
        }

        // Sau đó gửi event room-players-updated
        await BroadcastRoomPlayersUpdateAsync(roomCode, players);
    }

    /// <summary>
    /// Broadcast host change event
    /// </summary>
    public async Task BroadcastHostChangeAsync(string roomCode, GamePlayer newHost)
    {
        var eventData = new HostChangeEventData
        {
            NewHost = newHost.Username,
            NewHostId = newHost.UserId,
            Message = $"{newHost.Username} đã trở thành host mới"
        };

        await BroadcastToRoomAsync(roomCode, "host-changed", eventData);
    }

    /// <summary>
    /// Send welcome message to player
    /// </summary>
    public async Task SendWelcomeMessageAsync(string socketId, string roomCode, bool isHost, string message)
    {
        var eventData = new RoomJoinEventData
        {
            RoomCode = roomCode,
            IsHost = isHost,
            Message = message
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
    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        var messageObj = new {
            Type = eventName.ToUpper().Replace("-", "_"),
            Data = data,
            Timestamp = DateTime.UtcNow
        };
        var message = JsonSerializer.Serialize(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"[WEBSOCKET] Broadcasting to room {roomCode}: {message}");

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
                    Type = eventName.ToUpper().Replace("-", "_"),
                    Data = data,
                    Timestamp = DateTime.UtcNow
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

    /// <summary>
    /// Broadcast message đến tất cả WebSocket connections hiện tại
    /// Dùng khi cần gửi event mà không cần dựa vào in-memory game rooms
    /// </summary>
    public async Task BroadcastToAllConnectionsAsync(string roomCode, string eventName, object data)
    {
        var messageObj = new {
            Type = eventName.ToUpper().Replace("-", "_"),
            Data = data,
            Timestamp = DateTime.UtcNow
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
    /// Dùng để gửi player-joined event chỉ cho những người đã có trong phòng trước đó
    /// </summary>
    public async Task BroadcastToOthersAsync(string roomCode, int excludeUserId, string eventName, object data)
    {
        var messageObj = new {
            Type = eventName.ToUpper().Replace("-", "_"),
            Data = data,
            Timestamp = DateTime.UtcNow
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