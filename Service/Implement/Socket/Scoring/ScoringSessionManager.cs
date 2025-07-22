using System.Collections.Concurrent;
using System.Text.Json;
namespace ConsoleApp1.Service.Implement.Socket.Scoring;
/// <summary>
/// Service quản lý các scoring sessions
/// </summary>
public class ScoringSessionManager
{
    // Dictionary lưu trữ các phiên tính điểm
    private readonly ConcurrentDictionary<string, ScoringSession> _scoringSessions = new();
    /// <summary>
    /// Lấy hoặc tạo scoring session cho phòng
    /// </summary>
    public ScoringSession GetOrCreateSession(string roomCode)
    {
        if (!_scoringSessions.TryGetValue(roomCode, out var scoringSession))
        {
            scoringSession = new ScoringSession { RoomCode = roomCode };
            _scoringSessions[roomCode] = scoringSession;
        }
        return scoringSession;
    }
    /// <summary>
    /// Cập nhật điểm số cho players trong session
    /// </summary>
    public void UpdatePlayerScores(string roomCode, List<ScoreboardUpdateData> scoreboardData)
    {
        var scoringSession = GetOrCreateSession(roomCode);
        foreach (var playerData in scoreboardData)
        {
            if (!scoringSession.PlayerScores.TryGetValue(playerData.Username, out var playerScore))
            {
                playerScore = new PlayerScore { Username = playerData.Username };
                scoringSession.PlayerScores[playerData.Username] = playerScore;
            }
            // Cập nhật thông tin điểm số
            playerScore.TotalScore = playerData.Score;
            playerScore.CorrectAnswers = playerData.CorrectAnswers;
            playerScore.TotalAnswers = playerData.TotalAnswers;
            playerScore.AverageTime = playerData.AverageTime;
            playerScore.LastAnswerTime = DateTime.UtcNow;
        }
        scoringSession.LastUpdateTime = DateTime.UtcNow;
    }
    /// <summary>
    /// Lấy scoring session
    /// </summary>
    public ScoringSession? GetSession(string roomCode)
    {
        _scoringSessions.TryGetValue(roomCode, out var session);
        return session;
    }
    /// <summary>
    /// Đánh dấu game kết thúc
    /// </summary>
    public void EndGame(string roomCode)
    {
        if (_scoringSessions.TryGetValue(roomCode, out var scoringSession))
        {
            scoringSession.IsGameActive = false;
        }
    }
    /// <summary>
    /// Cleanup scoring session
    /// </summary>
    public void CleanupSession(string roomCode)
    {
        _scoringSessions.TryRemove(roomCode, out var _);
    }
    /// <summary>
    /// Parse scoreboard data từ object
    /// </summary>
    public List<ScoreboardUpdateData> ParseScoreboardData(object scoreboard)
    {
        try
        {
            var scoreboardJson = JsonSerializer.Serialize(scoreboard);
            var scoreboardData = JsonSerializer.Deserialize<List<ScoreboardUpdateData>>(scoreboardJson);
            return scoreboardData ?? new List<ScoreboardUpdateData>();
        }
        catch (Exception ex)
        {
            return new List<ScoreboardUpdateData>();
        }
    }
}
