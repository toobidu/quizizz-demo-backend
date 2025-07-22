using ConsoleApp1.Model.DTO.Game;
namespace ConsoleApp1.Service.Implement.Socket.Scoring;
/// <summary>
/// Service tính toán điểm số và xếp hạng
/// </summary>
public class ScoreCalculator
{
    private readonly AchievementCalculator _achievementCalculator;
    public ScoreCalculator()
    {
        _achievementCalculator = new AchievementCalculator();
    }
    /// <summary>
    /// Tính toán bảng xếp hạng từ scoring session
    /// </summary>
    public List<ScoreboardEntry> CalculateScoreboard(ScoringSession scoringSession)
    {
        return scoringSession.PlayerScores.Values
            .Select((playerScore, index) => new ScoreboardEntry
            {
                Username = playerScore.Username,
                Score = playerScore.TotalScore,
                Rank = 0, // Sẽ được tính sau khi sort
                CorrectAnswers = playerScore.CorrectAnswers,
                AverageTime = playerScore.AverageTime
            })
            .OrderByDescending(p => p.Score) // Sắp xếp theo điểm số giảm dần
            .ThenBy(p => p.AverageTime) // Nếu bằng điểm thì ai nhanh hơn lên trước
            .Select((entry, index) => {
                entry.Rank = index + 1;
                return entry;
            })
            .ToList();
    }
    /// <summary>
    /// Detect thay đổi vị trí trong bảng xếp hạng
    /// </summary>
    public List<object> DetectPositionChanges(List<ScoreboardEntry> oldScoreboard, List<ScoreboardEntry> newScoreboard)
    {
        var changes = new List<object>();
        if (oldScoreboard.Count == 0) return changes;
        foreach (var newEntry in newScoreboard)
        {
            var oldEntry = oldScoreboard.FirstOrDefault(o => o.Username == newEntry.Username);
            if (oldEntry != null && oldEntry.Rank != newEntry.Rank)
            {
                changes.Add(new {
                    username = newEntry.Username,
                    oldRank = oldEntry.Rank,
                    newRank = newEntry.Rank,
                    change = oldEntry.Rank > newEntry.Rank ? ScoringConstants.PositionChanges.Up : ScoringConstants.PositionChanges.Down
                });
            }
        }
        return changes;
    }
    /// <summary>
    /// Tính toán kết quả cuối game chi tiết
    /// </summary>
    public DetailedGameResults CalculateFinalResults(ScoringSession scoringSession)
    {
        var rankings = scoringSession.PlayerScores.Values
            .OrderByDescending(p => p.TotalScore)
            .ThenBy(p => p.AverageTime)
            .Select((player, index) => new {
                rank = index + 1,
                username = player.Username,
                totalScore = player.TotalScore,
                correctAnswers = player.CorrectAnswers,
                totalAnswers = player.TotalAnswers,
                accuracy = player.TotalAnswers > 0 ? (double)player.CorrectAnswers / player.TotalAnswers * 100 : 0,
                averageTime = player.AverageTime,
                maxStreak = player.MaxStreak,
                questionScores = player.QuestionScores
            })
            .ToList<object>();
        var statistics = new {
            totalPlayers = scoringSession.PlayerScores.Count,
            averageScore = scoringSession.PlayerScores.Values.Average(p => p.TotalScore),
            highestScore = scoringSession.PlayerScores.Values.Max(p => p.TotalScore),
            lowestScore = scoringSession.PlayerScores.Values.Min(p => p.TotalScore),
            averageAccuracy = scoringSession.PlayerScores.Values.Average(p => 
                p.TotalAnswers > 0 ? (double)p.CorrectAnswers / p.TotalAnswers * 100 : 0),
            averageTime = scoringSession.PlayerScores.Values.Average(p => p.AverageTime)
        };
        var achievements = _achievementCalculator.CalculateAchievements(scoringSession);
        // Estimate game start time (có thể cần lưu chính xác hơn)
        var gameStartTime = scoringSession.PlayerScores.Values
            .Select(p => p.LastAnswerTime)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min()
            .AddMinutes(-ScoringConstants.Thresholds.GameStartTimeEstimateMinutes);
        return new DetailedGameResults
        {
            Rankings = rankings,
            Statistics = statistics,
            Achievements = achievements,
            GameStartTime = gameStartTime
        };
    }
    /// <summary>
    /// Tính phần trăm player tốt hơn bao nhiêu người khác
    /// </summary>
    public double CalculateBetterThanPercent(PlayerScore playerScore, List<object> rankings)
    {
        var totalPlayers = rankings.Count;
        if (totalPlayers <= 1) return 0;
        var playerRank = rankings
            .Cast<dynamic>()
            .FirstOrDefault(r => r.username == playerScore.Username)?.rank ?? totalPlayers;
        var betterThanCount = totalPlayers - playerRank;
        return (double)betterThanCount / (totalPlayers - 1) * 100;
    }
}
