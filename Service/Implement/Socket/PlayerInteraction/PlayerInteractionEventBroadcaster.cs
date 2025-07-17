using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;

/// <summary>
/// Service broadcast events cho Player Interaction
/// </summary>
public class PlayerInteractionEventBroadcaster
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;

    public PlayerInteractionEventBroadcaster(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
    }

    /// <summary>
    /// Gửi kết quả câu trả lời cho người chơi
    /// </summary>
    public async Task SendAnswerResultAsync(string roomCode, string username, AnswerResultEventData eventData)
    {
        await SendToPlayerAsync(roomCode, username, PlayerInteractionConstants.Events.AnswerResult, eventData);
    }

    /// <summary>
    /// Gửi thông báo người chơi đã hoàn thành
    /// </summary>
    public async Task SendPlayerFinishedAsync(string roomCode, string username, PlayerFinishedEventData eventData)
    {
        await SendToPlayerAsync(roomCode, username, PlayerInteractionConstants.Events.PlayerFinished, eventData);
    }

    /// <summary>
    /// Phát sóng cập nhật bảng điểm
    /// </summary>
    public async Task BroadcastScoreboardUpdateAsync(string roomCode, ScoreboardUpdateEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, PlayerInteractionConstants.Events.ScoreboardUpdate, eventData);
    }

    /// <summary>
    /// Phát sóng thay đổi trạng thái người chơi
    /// </summary>
    public async Task BroadcastPlayerStatusChangeAsync(string roomCode, PlayerStatusEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, PlayerInteractionConstants.Events.PlayerStatusChanged, eventData);
    }

    /// <summary>
    /// Phát sóng câu hỏi đã hoàn thành
    /// </summary>
    public async Task BroadcastQuestionCompletedAsync(string roomCode, int questionIndex)
    {
        await BroadcastToRoomAsync(roomCode, PlayerInteractionConstants.Events.QuestionCompleted, new {
            questionIndex = questionIndex,
            message = PlayerInteractionConstants.Messages.AllPlayersAnswered
        });
    }

    /// <summary>
    /// Phát sóng game đã hoàn thành
    /// </summary>
    public async Task BroadcastGameCompletedAsync(string roomCode, GameCompletionEventData eventData)
    {
        await BroadcastToRoomAsync(roomCode, PlayerInteractionConstants.Events.GameCompleted, eventData);
    }

    /// <summary>
    /// Gửi lỗi cho người chơi
    /// </summary>
    public async Task SendErrorToPlayerAsync(string roomCode, string username, string errorMessage)
    {
        await SendToPlayerAsync(roomCode, username, PlayerInteractionConstants.Events.Error, new {
            message = errorMessage,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gửi message đến tất cả client trong phòng
    /// </summary>
    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
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
                        Console.WriteLine($"[PLAYER] Thất bại khi gửi tin nhắn đến {player.Username}: {ex.Message}");
                    }
                }
            });

        await Task.WhenAll(sendTasks);
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
                    eventName = eventName,
                    data = data
                });
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PLAYER] Thất bại khi gửi tin nhắn đến {username}: {ex.Message}");
            }
        }
    }
}