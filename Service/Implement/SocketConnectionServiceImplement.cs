using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class SocketConnectionServiceImplement : ISocketConnectionService
{
    private readonly Dictionary<string, WebSocket> _connections;
    private readonly Dictionary<string, string> _roomConnections; // socketId -> roomCode
    private readonly Dictionary<int, List<string>> _userConnections; // userId -> List<socketId>

    public SocketConnectionServiceImplement()
    {
        _connections = new Dictionary<string, WebSocket>();
        _roomConnections = new Dictionary<string, string>();
        _userConnections = new Dictionary<int, List<string>>();
    }

    public async Task BroadcastToRoomAsync(string roomCode, string eventType, object data)
    {
        try
        {
            var socketIds = _roomConnections
                .Where(kv => kv.Value == roomCode)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var socketId in socketIds)
            {
                await SendMessageAsync(socketId, eventType, data);
            }
        }
        catch (Exception)
        {
            // Log exception
        }
    }

    public async Task BroadcastToUserAsync(int userId, string eventType, object data)
    {
        try
        {
            if (_userConnections.TryGetValue(userId, out var socketIds))
            {
                foreach (var socketId in socketIds)
                {
                    await SendMessageAsync(socketId, eventType, data);
                }
            }
        }
        catch (Exception)
        {
            // Log exception
        }
    }

    public async Task BroadcastToSocketAsync(string socketId, string eventType, object data)
    {
        try
        {
            await SendMessageAsync(socketId, eventType, data);
        }
        catch (Exception)
        {
            // Log exception
        }
    }

    public async Task BroadcastToAllAsync(string eventType, object data)
    {
        try
        {
            foreach (var socketId in _connections.Keys)
            {
                await SendMessageAsync(socketId, eventType, data);
            }
        }
        catch (Exception)
        {
            // Log exception
        }
    }

    private async Task SendMessageAsync(string socketId, string eventType, object data)
    {
        if (!_connections.TryGetValue(socketId, out var socket) || socket.State != WebSocketState.Open)
        {
            return;
        }

        var message = new
        {
            type = eventType,
            data,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }
}