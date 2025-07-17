using ConsoleApp1.Model.DTO.Game;

namespace ConsoleApp1.Service.Implement.Socket.Scoring;

/// <summary>
/// Extension methods cho Scoring Services
/// </summary>
public static class ScoringServiceExtensions
{
    /// <summary>
    /// Kiểm tra xem player có đạt perfect score không
    /// </summary>
    public static bool HasPerfectScore(this PlayerScore playerScore)
    {
        return playerScore.TotalAnswers > 0 && playerScore.CorrectAnswers == playerScore.TotalAnswers;
    }

    /// <summary>
    /// Tính accuracy percentage
    /// </summary>
    public static double GetAccuracyPercentage(this PlayerScore playerScore)
    {
        return playerScore.TotalAnswers > 0 
            ? (double)playerScore.CorrectAnswers / playerScore.TotalAnswers * 100 
            : 0;
    }

    /// <summary>
    /// Kiểm tra xem session có đang active không
    /// </summary>
    public static bool IsActive(this ScoringSession session)
    {
        return session.IsGameActive && 
               session.PlayerScores.Count > 0 && 
               DateTime.UtcNow.Subtract(session.LastUpdateTime).TotalMinutes < 30; // 30 phút timeout
    }

    /// <summary>
    /// Lấy top N players
    /// </summary>
    public static List<ScoreboardEntry> GetTopPlayers(this List<ScoreboardEntry> scoreboard, int count)
    {
        return scoreboard.Take(count).ToList();
    }

    /// <summary>
    /// Tìm player trong scoreboard
    /// </summary>
    public static ScoreboardEntry? FindPlayer(this List<ScoreboardEntry> scoreboard, string username)
    {
        return scoreboard.FirstOrDefault(entry => entry.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Kiểm tra xem có thay đổi vị trí không
    /// </summary>
    public static bool HasPositionChanges(this List<object> positionChanges)
    {
        return positionChanges.Count > 0;
    }

    /// <summary>
    /// Format thời gian hiển thị
    /// </summary>
    public static string FormatTime(this double seconds)
    {
        if (seconds < 60)
            return $"{seconds:F1}s";
        
        var minutes = (int)(seconds / 60);
        var remainingSeconds = seconds % 60;
        return $"{minutes}m {remainingSeconds:F1}s";
    }

    /// <summary>
    /// Tính điểm trung bình của session
    /// </summary>
    public static double GetAverageScore(this ScoringSession session)
    {
        return session.PlayerScores.Count > 0 
            ? session.PlayerScores.Values.Average(p => p.TotalScore) 
            : 0;
    }

    /// <summary>
    /// Lấy player có điểm cao nhất
    /// </summary>
    public static PlayerScore? GetTopPlayer(this ScoringSession session)
    {
        return session.PlayerScores.Values
            .OrderByDescending(p => p.TotalScore)
            .ThenBy(p => p.AverageTime)
            .FirstOrDefault();
    }

    /// <summary>
    /// Kiểm tra xem có đủ players để bắt đầu game không
    /// </summary>
    public static bool HasMinimumPlayers(this ScoringSession session, int minimumCount = 2)
    {
        return session.PlayerScores.Count >= minimumCount;
    }

    /// <summary>
    /// Tạo summary statistics
    /// </summary>
    public static object CreateSummaryStats(this ScoringSession session)
    {
        if (session.PlayerScores.Count == 0)
        {
            return new { message = "No players in session" };
        }

        var scores = session.PlayerScores.Values.Select(p => p.TotalScore).ToList();
        var accuracies = session.PlayerScores.Values.Select(p => p.GetAccuracyPercentage()).ToList();
        var times = session.PlayerScores.Values.Where(p => p.AverageTime > 0).Select(p => p.AverageTime).ToList();

        return new {
            totalPlayers = session.PlayerScores.Count,
            averageScore = scores.Average(),
            highestScore = scores.Max(),
            lowestScore = scores.Min(),
            averageAccuracy = accuracies.Average(),
            averageTime = times.Count > 0 ? times.Average() : 0,
            perfectScorePlayers = session.PlayerScores.Values.Count(p => p.HasPerfectScore())
        };
    }
}