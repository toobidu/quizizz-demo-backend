using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Implementation của ISocketMessageSender
/// Chịu trách nhiệm gửi message qua WebSocket
/// </summary>
public class SocketMessageSender : ISocketMessageSender
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    public SocketMessageSender(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
    }
    /// <summary>
    /// Gửi message đến tất cả client trong phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="eventName">Tên event</param>
    /// <param name="data">Dữ liệu gửi kèm</param>
    public async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) 
        {
            return;
        }
        var message = JsonSerializer.Serialize(new {
            eventName = eventName,
            data = data,
            timestamp = DateTime.UtcNow
        });
        var buffer = Encoding.UTF8.GetBytes(message);
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
                    }
                }
                else
                {
                }
            });
        await Task.WhenAll(sendTasks);
    }
    /// <summary>
    /// Gửi message đến một player cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Username của player</param>
    /// <param name="eventName">Tên event</param>
    /// <param name="data">Dữ liệu gửi kèm</param>
    public async Task SendToPlayerAsync(string roomCode, string username, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) 
        {
            return;
        }
        var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
        if (player?.SocketId == null) 
        {
            return;
        }
        if (_connections.TryGetValue(player.SocketId, out var socket) && socket.State == WebSocketState.Open)
        {
            try
            {
                var message = JsonSerializer.Serialize(new {
                    eventName = eventName,
                    data = data,
                    timestamp = DateTime.UtcNow
                });
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
            }
        }
        else
        {
        }
    }
    /// <summary>
    /// Gửi message đến nhiều players cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="usernames">Danh sách username</param>
    /// <param name="eventName">Tên event</param>
    /// <param name="data">Dữ liệu gửi kèm</param>
    public async Task SendToPlayersAsync(string roomCode, IEnumerable<string> usernames, string eventName, object data)
    {
        var sendTasks = usernames.Select(username => 
            SendToPlayerAsync(roomCode, username, eventName, data));
        await Task.WhenAll(sendTasks);
    }
    /// <summary>
    /// Gửi message đến tất cả players trừ một số players cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="excludeUsernames">Danh sách username cần loại trừ</param>
    /// <param name="eventName">Tên event</param>
    /// <param name="data">Dữ liệu gửi kèm</param>
    public async Task BroadcastToRoomExceptAsync(string roomCode, IEnumerable<string> excludeUsernames, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        var excludeSet = excludeUsernames.ToHashSet();
        var targetPlayers = gameRoom.Players
            .Where(p => !excludeSet.Contains(p.Username))
            .Select(p => p.Username);
        await SendToPlayersAsync(roomCode, targetPlayers, eventName, data);
    }
}
