using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;

namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;

/// <summary>
/// Service quản lý trạng thái players
/// </summary>
public class PlayerStatusManager
{
    private readonly ConcurrentDictionary<string, PlayerGameSession> _gameSessions;
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;

    public PlayerStatusManager(
        ConcurrentDictionary<string, PlayerGameSession> gameSessions,
        ConcurrentDictionary<string, GameRoom> gameRooms)
    {
        _gameSessions = gameSessions;
        _gameRooms = gameRooms;
    }

    /// <summary>
    /// Cập nhật trạng thái player
    /// </summary>
    public bool UpdatePlayerStatus(string roomCode, string username, string status)
    {
        try
        {
            // Cập nhật trong game session
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                if (gameSession.PlayerResults.TryGetValue(username, out var playerResult))
                {
                    var oldStatus = playerResult.Status;
                    playerResult.Status = status;
                    
                    Console.WriteLine($"[PLAYER] {username} status changed from {oldStatus} to {status}");
                }
            }

            // Cập nhật trong game room
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
                if (player != null)
                {
                    player.Status = status;
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi cập nhật trạng thái cho {username}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lấy trạng thái player
    /// </summary>
    public string GetPlayerStatus(string roomCode, string username)
    {
        try
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                if (gameSession.PlayerResults.TryGetValue(username, out var playerResult))
                {
                    return playerResult.Status;
                }
            }

            return PlayerInteractionConstants.PlayerStatuses.Waiting;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi lấy trạng thái cho {username}: {ex.Message}");
            return PlayerInteractionConstants.PlayerStatuses.Waiting;
        }
    }

    /// <summary>
    /// Kiểm tra tất cả players đã trả lời câu hỏi chưa
    /// </summary>
    public bool CheckQuestionCompletion(string roomCode, int questionIndex)
    {
        try
        {
            if (!_gameSessions.TryGetValue(roomCode, out var gameSession)) return false;
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return false;
            
            var totalPlayers = gameRoom.Players.Count;
            var answeredPlayers = gameSession.PlayerResults.Values
                .Count(p => p.Answers.Any(a => a.QuestionIndex == questionIndex));
            
            Console.WriteLine($"[PLAYER] Question {questionIndex}: {answeredPlayers}/{totalPlayers} players answered");
            
            return answeredPlayers >= totalPlayers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi kiểm tra hoàn thành câu hỏi: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra tất cả players đã hoàn thành game chưa
    /// </summary>
    public bool CheckAllPlayersFinished(string roomCode)
    {
        try
        {
            if (!_gameSessions.TryGetValue(roomCode, out var gameSession)) return false;
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return false;
            
            var totalPlayers = gameRoom.Players.Count;
            var finishedPlayers = gameSession.PlayerResults.Values
                .Count(p => p.Status == PlayerInteractionConstants.PlayerStatuses.Finished);
            
            Console.WriteLine($"[PLAYER] Game progress: {finishedPlayers}/{totalPlayers} players finished");
            
            return finishedPlayers >= totalPlayers && gameSession.IsGameActive;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi kiểm tra tất cả người chơi đã hoàn thành: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Lấy danh sách players và trạng thái
    /// </summary>
    public List<object> GetPlayersStatus(string roomCode)
    {
        try
        {
            if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
                return new List<object>();

            return gameSession.PlayerResults.Values
                .Select(p => new {
                    username = p.Username,
                    status = p.Status,
                    score = p.Score,
                    answersCount = p.Answers.Count,
                    lastAnswerTime = p.LastAnswerTime
                })
                .Cast<object>()
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi lấy trạng thái người chơi: {ex.Message}");
            return new List<object>();
        }
    }

    /// <summary>
    /// Đánh dấu player đã hoàn thành
    /// </summary>
    public void MarkPlayerFinished(string roomCode, string username)
    {
        UpdatePlayerStatus(roomCode, username, PlayerInteractionConstants.PlayerStatuses.Finished);
    }

    /// <summary>
    /// Reset trạng thái tất cả players
    /// </summary>
    public void ResetAllPlayersStatus(string roomCode, string newStatus = PlayerInteractionConstants.PlayerStatuses.Waiting)
    {
        try
        {
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                foreach (var playerResult in gameSession.PlayerResults.Values)
                {
                    playerResult.Status = newStatus;
                }
            }

            Console.WriteLine($"[PLAYER] Reset all players status to {newStatus} for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Lỗi đặt lại trạng thái người chơi: {ex.Message}");
        }
    }
}