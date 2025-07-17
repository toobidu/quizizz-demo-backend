namespace ConsoleApp1.Service.Implement.Socket.Scoring;

/// <summary>
/// Service tính toán achievements riêng biệt
/// </summary>
public class AchievementCalculator
{
    /// <summary>
    /// Tính toán tất cả achievements cho session
    /// </summary>
    public List<object> CalculateAchievements(ScoringSession scoringSession)
    {
        var achievements = new List<object>();

        // Perfect Score Achievement
        achievements.AddRange(CalculatePerfectScoreAchievements(scoringSession));

        // Speed Demon Achievement
        var speedAchievement = CalculateSpeedDemonAchievement(scoringSession);
        if (speedAchievement != null)
            achievements.Add(speedAchievement);

        // Streak Master Achievement
        var streakAchievement = CalculateStreakMasterAchievement(scoringSession);
        if (streakAchievement != null)
            achievements.Add(streakAchievement);

        // Có thể thêm các achievements khác ở đây
        achievements.AddRange(CalculateAdditionalAchievements(scoringSession));

        return achievements;
    }

    /// <summary>
    /// Tính Perfect Score achievements
    /// </summary>
    private List<object> CalculatePerfectScoreAchievements(ScoringSession scoringSession)
    {
        var achievements = new List<object>();
        
        var perfectPlayers = scoringSession.PlayerScores.Values
            .Where(p => p.TotalAnswers > 0 && p.CorrectAnswers == p.TotalAnswers)
            .ToList();
        
        foreach (var player in perfectPlayers)
        {
            achievements.Add(new {
                username = player.Username,
                achievement = ScoringConstants.Achievements.PerfectScore,
                description = "Trả lời đúng tất cả câu hỏi!",
                icon = ScoringConstants.Icons.Trophy
            });
        }

        return achievements;
    }

    /// <summary>
    /// Tính Speed Demon achievement
    /// </summary>
    private object? CalculateSpeedDemonAchievement(ScoringSession scoringSession)
    {
        var fastestPlayer = scoringSession.PlayerScores.Values
            .Where(p => p.AverageTime > 0)
            .OrderBy(p => p.AverageTime)
            .FirstOrDefault();
        
        if (fastestPlayer != null)
        {
            return new {
                username = fastestPlayer.Username,
                achievement = ScoringConstants.Achievements.SpeedDemon,
                description = $"Trả lời nhanh nhất với thời gian trung bình {fastestPlayer.AverageTime:F1}s",
                icon = ScoringConstants.Icons.Lightning
            };
        }

        return null;
    }

    /// <summary>
    /// Tính Streak Master achievement
    /// </summary>
    private object? CalculateStreakMasterAchievement(ScoringSession scoringSession)
    {
        var streakMaster = scoringSession.PlayerScores.Values
            .Where(p => p.MaxStreak >= ScoringConstants.Thresholds.MinStreakForAchievement)
            .OrderByDescending(p => p.MaxStreak)
            .FirstOrDefault();
        
        if (streakMaster != null)
        {
            return new {
                username = streakMaster.Username,
                achievement = ScoringConstants.Achievements.StreakMaster,
                description = $"Chuỗi trả lời đúng dài nhất: {streakMaster.MaxStreak} câu",
                icon = ScoringConstants.Icons.Fire
            };
        }

        return null;
    }

    /// <summary>
    /// Tính các achievements bổ sung (có thể mở rộng)
    /// </summary>
    private List<object> CalculateAdditionalAchievements(ScoringSession scoringSession)
    {
        var achievements = new List<object>();

        // Comeback King - người lên hạng nhiều nhất
        var comebackPlayer = FindComebackPlayer(scoringSession);
        if (comebackPlayer != null)
        {
            achievements.Add(new {
                username = comebackPlayer.Username,
                achievement = "Comeback King",
                description = "Lên hạng ấn tượng nhất trong game!",
                icon = "👑"
            });
        }

        // Consistent Player - độ lệch chuẩn thấp nhất
        var consistentPlayer = FindConsistentPlayer(scoringSession);
        if (consistentPlayer != null)
        {
            achievements.Add(new {
                username = consistentPlayer.Username,
                achievement = "Consistent Player",
                description = "Điểm số ổn định nhất qua các câu hỏi!",
                icon = "📊"
            });
        }

        return achievements;
    }

    /// <summary>
    /// Tìm player có comeback tốt nhất
    /// </summary>
    private PlayerScore? FindComebackPlayer(ScoringSession scoringSession)
    {
        // Logic để tìm player lên hạng nhiều nhất
        // Cần thêm tracking position changes trong tương lai
        return null;
    }

    /// <summary>
    /// Tìm player có điểm số ổn định nhất
    /// </summary>
    private PlayerScore? FindConsistentPlayer(ScoringSession scoringSession)
    {
        if (scoringSession.PlayerScores.Count < 2) return null;

        var mostConsistent = scoringSession.PlayerScores.Values
            .Where(p => p.QuestionScores.Count > 1)
            .Select(p => new {
                Player = p,
                StandardDeviation = CalculateStandardDeviation(p.QuestionScores)
            })
            .Where(x => x.StandardDeviation > 0)
            .OrderBy(x => x.StandardDeviation)
            .FirstOrDefault();

        return mostConsistent?.Player;
    }

    /// <summary>
    /// Tính độ lệch chuẩn
    /// </summary>
    private double CalculateStandardDeviation(List<int> scores)
    {
        if (scores.Count < 2) return 0;

        var average = scores.Average();
        var sumOfSquares = scores.Sum(score => Math.Pow(score - average, 2));
        return Math.Sqrt(sumOfSquares / scores.Count);
    }
}