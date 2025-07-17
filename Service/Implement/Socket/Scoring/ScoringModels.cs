using ConsoleApp1.Model.DTO.Game;

namespace ConsoleApp1.Service.Implement.Socket.Scoring;

/// <summary>
/// Class quản lý scoring session của một phòng
/// </summary>
public class ScoringSession
{
    public string RoomCode { get; set; } = string.Empty;
    public Dictionary<string, PlayerScore> PlayerScores { get; set; } = new();
    public List<ScoreboardEntry> CurrentScoreboard { get; set; } = new();
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
    public bool IsGameActive { get; set; } = true;
}

/// <summary>
/// Class lưu trữ điểm số chi tiết của từng player
/// </summary>
public class PlayerScore
{
    public string Username { get; set; } = string.Empty;
    public int TotalScore { get; set; } = 0;
    public int CorrectAnswers { get; set; } = 0;
    public int TotalAnswers { get; set; } = 0;
    public double AverageTime { get; set; } = 0;
    public int CurrentStreak { get; set; } = 0; // Chuỗi trả lời đúng liên tiếp
    public int MaxStreak { get; set; } = 0; // Chuỗi dài nhất
    public List<int> QuestionScores { get; set; } = new(); // Điểm từng câu
    public DateTime LastAnswerTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Class để parse scoreboard update data
/// </summary>
public class ScoreboardUpdateData
{
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
    public double AverageTime { get; set; }
}

/// <summary>
/// Class chứa kết quả cuối game chi tiết
/// </summary>
public class DetailedGameResults
{
    public List<object> Rankings { get; set; } = new();
    public object Statistics { get; set; } = new();
    public List<object> Achievements { get; set; } = new();
    public DateTime GameStartTime { get; set; } = DateTime.UtcNow;
}