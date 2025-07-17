using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket.Scoring;

/// <summary>
/// Service gửi messages qua WebSocket
/// </summary>
public class SocketMessageSender
{
    // Dictionary lưu trữ các phòng game (chia sẻ với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    
    // Dictionary lưu trữ các kết nối WebSocket (chia sẻ với ConnectionService)
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
    public async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var message = JsonSerializer.Serialize(new {
            eventName = eventName,
            data = data
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
                        Console.WriteLine($"[SCORING] Thất bại gửi tin nhắn tới {player.Username}: {ex.Message}");
                    }
                }
            });
        
        await Task.WhenAll(sendTasks);
    }

    /// <summary>
    /// Gửi message đến một player cụ thể
    /// </summary>
    public async Task SendToPlayerAsync(string roomCode, string username, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
        if (player?.SocketId == null) return;
        
        if (_connections.TryGetValue(player.SocketId, out var socket) && socket.State == WebSocketState.Open)
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
                Console.WriteLine($"[SCORING] Thất bại gửi tin nhắn tới {username}: {ex.Message}");
            }
        }
    }
}