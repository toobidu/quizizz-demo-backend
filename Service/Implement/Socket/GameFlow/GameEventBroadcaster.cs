using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
namespace ConsoleApp1.Service.Implement.Socket.GameFlow;
/// <summary>
/// Service phát sóng sự kiện cho Game Flow
/// </summary>
public class GameEventBroadcaster
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    public GameEventBroadcaster(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
    }
    /// <summary>
    /// Phát sóng sự kiện game bắt đầu
    /// </summary>
    public async Task BroadcastGameStartedAsync(string roomCode, GameStartEventData eventData)
    {
        Console.WriteLine($"📡 [GameEventBroadcaster] Broadcasting game-started to room {roomCode}");
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.GameStarted, eventData);
        Console.WriteLine($"📡 [GameEventBroadcaster] game-started broadcast completed for room {roomCode}");
    }
    /// <summary>
    /// Phát sóng câu hỏi mới
    /// </summary>
    public async Task BroadcastNewQuestionAsync(string roomCode, QuestionEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.NewQuestion, eventData);
    }
    /// <summary>
    /// Phát sóng cập nhật thời gian
    /// </summary>
    public async Task BroadcastTimerUpdateAsync(string roomCode, TimerUpdateEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.TimerUpdate, eventData);
    }
    /// <summary>
    /// Phát sóng đếm ngược
    /// </summary>
    public async Task BroadcastCountdownAsync(string roomCode, CountdownEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.Countdown, eventData);
    }
    /// <summary>
    /// Phát sóng cập nhật tiến độ
    /// </summary>
    public async Task BroadcastProgressUpdateAsync(string roomCode, ProgressUpdateEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.ProgressUpdate, eventData);
    }
    /// <summary>
    /// Phát sóng game kết thúc
    /// </summary>
    public async Task BroadcastGameEndedAsync(string roomCode, GameEndEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.GameEnded, eventData);
    }
    /// <summary>
    /// Phát sóng thay đổi trạng thái game
    /// </summary>
    public async Task BroadcastGameStateChangedAsync(string roomCode, string gameState)
    {
        await BroadcastToRoomAsync(roomCode, GameFlowConstants.Events.GameStateChanged, new {
            gameState = gameState,
            timestamp = DateTime.UtcNow
        });
    }
    /// <summary>
    /// Gửi câu hỏi tiếp theo cho người chơi cụ thể
    /// </summary>
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username, QuestionEventData eventData)
    {
        await SendToPlayerAsync(roomCode, username, GameFlowConstants.Events.NextQuestion, eventData);
    }
    /// <summary>
    /// Gửi tiến độ người chơi cho người chơi cụ thể
    /// </summary>
    public async Task SendPlayerProgressAsync(string roomCode, string username, PlayerProgressEventData eventData)
    {
        await SendToPlayerAsync(roomCode, username, GameFlowConstants.Events.PlayerProgress, eventData);
    }
    /// <summary>
    /// Gửi thông báo người chơi hoàn thành
    /// </summary>
    public async Task SendPlayerFinishedAsync(string roomCode, string username, object data)
    {
        await SendToPlayerAsync(roomCode, username, GameFlowConstants.Events.PlayerFinished, data);
    }
    /// <summary>
    /// Gửi message đến tất cả client trong phòng
    /// </summary>
    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) 
        {
            Console.WriteLine($"❌ [GameEventBroadcaster] Room {roomCode} not found in _gameRooms");
            return;
        }
        
        Console.WriteLine($"📡 [GameEventBroadcaster] Broadcasting {eventName} to {gameRoom.Players.Count} players in room {roomCode}");
        
        var message = JsonSerializer.Serialize(new {
            type = eventName,
            data = data,
            timestamp = DateTime.UtcNow
        });
        var buffer = Encoding.UTF8.GetBytes(message);
        
        int sentCount = 0;
        foreach (var player in gameRoom.Players.Where(p => !string.IsNullOrEmpty(p.SocketId)))
        {
            if (_connections.TryGetValue(player.SocketId!, out var socket) && socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    sentCount++;
                    Console.WriteLine($"✅ [GameEventBroadcaster] Sent {eventName} to player {player.Username} (socketId: {player.SocketId})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ [GameEventBroadcaster] Failed to send {eventName} to player {player.Username}: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"⚠️ [GameEventBroadcaster] Player {player.Username} has invalid socket connection");
            }
        }
        Console.WriteLine($"📡 [GameEventBroadcaster] Broadcast {eventName} completed: {sentCount}/{gameRoom.Players.Count} players notified");
    }
    /// <summary>
    /// Gửi message đến một player cụ thể
    /// </summary>
    private async Task SendToPlayerAsync(string roomCode, string username, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
        if (player?.SocketId == null) return;
        if (_connections.TryGetValue(player.SocketId, out var socket) && socket.State == WebSocketState.Open)
        {
            try
            {
                var message = JsonSerializer.Serialize(new {
                    type = eventName,
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
    }
}
