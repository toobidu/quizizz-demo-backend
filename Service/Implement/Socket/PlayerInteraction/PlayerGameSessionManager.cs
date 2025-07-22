using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Text.Json;
namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;
/// <summary>
/// Service quản lý player game sessions
/// </summary>
public class PlayerGameSessionManager
{
    private readonly ConcurrentDictionary<string, PlayerGameSession> _gameSessions;
    public PlayerGameSessionManager(ConcurrentDictionary<string, PlayerGameSession> gameSessions)
    {
        _gameSessions = gameSessions;
    }
    /// <summary>
    /// Lấy game session
    /// </summary>
    public PlayerGameSession? GetGameSession(string roomCode)
    {
        _gameSessions.TryGetValue(roomCode, out var session);
        return session;
    }
    /// <summary>
    /// Kiểm tra game session có active không
    /// </summary>
    public bool IsGameSessionActive(string roomCode)
    {
        return _gameSessions.TryGetValue(roomCode, out var session) && session.IsGameActive;
    }
    /// <summary>
    /// Lấy hoặc tạo player result
    /// </summary>
    public PlayerGameResult GetOrCreatePlayerResult(string roomCode, string username)
    {
        var gameSession = GetGameSession(roomCode);
        if (gameSession == null) return new PlayerGameResult { Username = username };
        if (!gameSession.PlayerResults.TryGetValue(username, out var playerResult))
        {
            playerResult = new PlayerGameResult { Username = username };
            gameSession.PlayerResults[username] = playerResult;
        }
        return playerResult;
    }
    /// <summary>
    /// Cập nhật player result
    /// </summary>
    public void UpdatePlayerResult(string roomCode, string username, Action<PlayerGameResult> updateAction)
    {
        var playerResult = GetOrCreatePlayerResult(roomCode, username);
        updateAction(playerResult);
    }
    /// <summary>
    /// Parse answer submission từ JSON
    /// </summary>
    public PlayerAnswerSubmission? ParseAnswerSubmission(object answer)
    {
        try
        {
            var answerJson = JsonSerializer.Serialize(answer);
            return JsonSerializer.Deserialize<PlayerAnswerSubmission>(answerJson);
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    /// <summary>
    /// Tạo scoreboard từ player results
    /// </summary>
    public List<object> CreateScoreboard(string roomCode)
    {
        var gameSession = GetGameSession(roomCode);
        if (gameSession == null) return new List<object>();
        return gameSession.PlayerResults.Values
            .Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count,
                status = p.Status
            })
            .OrderByDescending(p => p.score)
            .Cast<object>()
            .ToList();
    }
    /// <summary>
    /// Tạo final results
    /// </summary>
    public List<object> CreateFinalResults(string roomCode)
    {
        var gameSession = GetGameSession(roomCode);
        if (gameSession == null) return new List<object>();
        return gameSession.PlayerResults.Values
            .Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count,
                correctAnswers = p.Answers.Count(a => a.IsCorrect),
                averageTime = p.Answers.Count > 0 ? p.Answers.Average(a => a.TimeToAnswer) : 0
            })
            .OrderByDescending(p => p.score)
            .Cast<object>()
            .ToList();
    }
    /// <summary>
    /// End game session
    /// </summary>
    public void EndGameSession(string roomCode)
    {
        if (_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            gameSession.IsGameActive = false;
        }
    }
    /// <summary>
    /// Cleanup game session
    /// </summary>
    public void CleanupGameSession(string roomCode)
    {
        if (_gameSessions.TryRemove(roomCode, out var gameSession))
        {
        }
    }
    /// <summary>
    /// Lấy session statistics
    /// </summary>
    public object GetSessionStatistics()
    {
        var totalSessions = _gameSessions.Count;
        var activeSessions = _gameSessions.Values.Count(s => s.IsGameActive);
        return new
        {
            totalSessions,
            activeSessions,
            averagePlayersPerSession = _gameSessions.Values
                .Where(s => s.PlayerResults.Count > 0)
                .DefaultIfEmpty()
                .Average(s => s?.PlayerResults.Count ?? 0)
        };
    }
}
