using ConsoleApp1.Model.DTO.Game;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket.Scoring;

/// <summary>
/// Service format dữ liệu điểm số cho client
/// </summary>
public class ScoreFormatter
{
    private readonly ScoreCalculator _scoreCalculator;

    public ScoreFormatter(ScoreCalculator scoreCalculator)
    {
        _scoreCalculator = scoreCalculator;
    }

    /// <summary>
    /// Format scoreboard cho client
    /// </summary>
    public object FormatScoreboardForClient(object scoreboard)
    {
        try
        {
            var scoreboardJson = JsonSerializer.Serialize(scoreboard);
            var scoreboardList = JsonSerializer.Deserialize<List<ScoreboardEntry>>(scoreboardJson);
            
            if (scoreboardList != null)
            {
                return scoreboardList.Select(entry => new {
                    username = entry.Username,
                    score = entry.Score,
                    rank = entry.Rank,
                    correctAnswers = entry.CorrectAnswers,
                    averageTime = entry.AverageTime,
                    displayTime = $"{entry.AverageTime:F1}s"
                }).Cast<object>().ToList();
            }
            return new List<object>();
        }
        catch
        {
            return scoreboard;
        }
    }

    /// <summary>
    /// Tạo scoreboard cá nhân hóa cho một player
    /// </summary>
    public object CreatePersonalizedScoreboard(object scoreboard, string username)
    {
        return new {
            scoreboard = scoreboard,
            highlightPlayer = username,
            timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Tạo kết quả cá nhân cho một player
    /// </summary>
    public object CreatePersonalResult(PlayerScore playerScore, DetailedGameResults detailedResults)
    {
        var playerRanking = detailedResults.Rankings
            .Cast<dynamic>()
            .FirstOrDefault(r => r.username == playerScore.Username);

        var playerAchievements = detailedResults.Achievements
            .Cast<dynamic>()
            .Where(a => a.username == playerScore.Username)
            .ToList();

        return new {
            personalStats = new {
                rank = playerRanking?.rank ?? 0,
                totalScore = playerScore.TotalScore,
                correctAnswers = playerScore.CorrectAnswers,
                totalAnswers = playerScore.TotalAnswers,
                accuracy = playerScore.TotalAnswers > 0 ? (double)playerScore.CorrectAnswers / playerScore.TotalAnswers * 100 : 0,
                averageTime = playerScore.AverageTime,
                maxStreak = playerScore.MaxStreak,
                questionScores = playerScore.QuestionScores
            },
            achievements = playerAchievements,
            comparison = new {
                betterThanPercent = _scoreCalculator.CalculateBetterThanPercent(playerScore, detailedResults.Rankings),
                scoreVsAverage = playerScore.TotalScore - (detailedResults.Statistics as dynamic)?.averageScore ?? 0
            }
        };
    }
}