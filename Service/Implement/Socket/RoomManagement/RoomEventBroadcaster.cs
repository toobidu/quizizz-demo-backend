using ConsoleApp1.Model.DTO.Game;
using ConsoleApp1.Model.DTO.WebSocket;
using ConsoleApp1.Config;
using ConsoleApp1.Service.Helper;
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
        // Sử dụng WebSocketEventHelper để tạo event chuẩn hóa
        var eventMessage = WebSocketEventHelper.CreateRoomPlayersUpdatedEvent(roomCode, players);
        // Chỉ gửi một lần duy nhất cho mỗi phòng
        await BroadcastToRoomAsync(roomCode, RoomManagementConstants.Events.RoomPlayersUpdated, eventMessage.Data!, true);
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
            var playerJoinedEvent = WebSocketEventHelper.CreatePlayerJoinedEvent(
                newPlayer.UserId, 
                newPlayer.Username, 
                roomCode, 
                newPlayer.IsHost
            );
            await BroadcastToRoomAsync(roomCode, RoomManagementConstants.Events.PlayerJoined, playerJoinedEvent.Data!);
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
        await BroadcastToRoomAsync(roomCode, RoomManagementConstants.Events.PlayerLeft, playerLeftData);
        // Đợi một chút để đảm bảo client xử lý sự kiện player-left trước
        await Task.Delay(200);
        // Sau đó gửi event RoomPlayersUpdated
        await BroadcastRoomPlayersUpdateAsync(roomCode, players);
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
        await BroadcastToRoomAsync(roomCode, RoomManagementConstants.Events.HostChanged, eventData);
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
        await SendToPlayerAsync(socketId, RoomManagementConstants.Events.RoomJoined, eventData);
    }
    /// <summary>
    /// Broadcast player-joined event to other players in room
    /// </summary>
    public async Task BroadcastPlayerJoinedEventAsync(string roomCode, object playerData)
    {
        await BroadcastToRoomAsync(roomCode, RoomManagementConstants.Events.PlayerJoined, playerData);
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
        if (preventDuplicates && eventName == RoomManagementConstants.Events.RoomPlayersUpdated)
        {
            // Tạo key để theo dõi sự kiện đã gửi
            string cacheKey = $"last_update_{roomCode}";
            // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 1 giây)
            if (_lastUpdateTimes.TryGetValue(cacheKey, out var lastTime) && 
                (DateTime.UtcNow - lastTime).TotalMilliseconds < 1000)
            {
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
        // Sử dụng JsonSerializerConfig để đảm bảo camelCase format
        var message = JsonSerializerConfig.SerializeCamelCase(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);
        // Log chi tiết hơn về tin nhắn được gửi
        if (eventName == "room-players-updated" || eventName == "player-joined")
        {
            // Kiểm tra định dạng dữ liệu
            if (eventName == "room-players-updated")
            {
                try
                {
                    var dataObj = data.GetType().GetProperty("players")?.GetValue(data);
                    var count = dataObj?.GetType().GetProperty("Count")?.GetValue(dataObj);
                    // Kiểm tra xem có trường totalPlayers không
                    var totalPlayers = data.GetType().GetProperty("totalPlayers")?.GetValue(data);
                    // Kiểm tra xem có trường maxPlayers không
                    var maxPlayers = data.GetType().GetProperty("maxPlayers")?.GetValue(data);
                }
                catch (Exception ex)
                {
                }
            }
        }
        // Kiểm tra xem có phòng trong _gameRooms không
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom) || gameRoom.Players.Count == 0)
        {
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
                    }
                });
            await Task.WhenAll(broadcastTasks);
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
                    }
                    catch (Exception ex)
                    {
                    }
                }
                else
                {
                }
            });
        await Task.WhenAll(sendTasks);
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
                // Sử dụng JsonSerializerConfig để đảm bảo camelCase format
                var message = JsonSerializerConfig.SerializeCamelCase(messageObj);
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                if (eventName == "room-players-updated" || eventName == "room-joined")
                {
                }
            }
            catch (Exception ex)
            {
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
        // Sử dụng JsonSerializerConfig để đảm bảo camelCase format
        var message = JsonSerializerConfig.SerializeCamelCase(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);
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
                }
            });
        await Task.WhenAll(sendTasks);
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
        // Sử dụng JsonSerializerConfig để đảm bảo camelCase format
        var message = JsonSerializerConfig.SerializeCamelCase(messageObj);
        var buffer = Encoding.UTF8.GetBytes(message);
        // Kiểm tra xem có phòng trong _gameRooms không
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom) || gameRoom.Players.Count == 0)
        {
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
                    }
                });
            await Task.WhenAll(broadcastTasks);
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
                    }
                    catch (Exception ex)
                    {
                    }
                }
            });
        await Task.WhenAll(sendTasks);
    }
}
