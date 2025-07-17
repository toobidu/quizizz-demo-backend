using ConsoleApp1.Model.DTO.Game;

namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;

/// <summary>
/// Models cho Player Interaction Service
/// </summary>

/// <summary>
/// Class nội bộ để quản lý session game của phòng
/// </summary>
public class PlayerGameSession
{
    public string RoomCode { get; set; } = string.Empty;
    public List<QuestionData> Questions { get; set; } = new();
    public Dictionary<string, PlayerGameResult> PlayerResults { get; set; } = new();
    public bool IsGameActive { get; set; } = false;
    public DateTime GameStartTime { get; set; }
    public int GameTimeLimit { get; set; } = 300;
}

/// <summary>
/// Class lưu trữ kết quả game của từng người chơi
/// </summary>
public class PlayerGameResult
{
    public string Username { get; set; } = string.Empty;
    public List<PlayerAnswer> Answers { get; set; } = new();
    public int Score { get; set; } = 0;
    public DateTime? LastAnswerTime { get; set; }
    public string Status { get; set; } = PlayerInteractionConstants.PlayerStatuses.Waiting;
}

/// <summary>
/// Class để parse câu trả lời từ client
/// </summary>
public class PlayerAnswerSubmission
{
    public int QuestionIndex { get; set; }
    public object SelectedAnswer { get; set; } = new();
    public long SubmitTime { get; set; }
}

/// <summary>
/// Answer result event data
/// </summary>
public class AnswerResultEventData
{
    public int QuestionIndex { get; set; }
    public bool IsCorrect { get; set; }
    public object CorrectAnswer { get; set; } = new();
    public int PointsEarned { get; set; }
    public int TotalScore { get; set; }
    public int TimeToAnswer { get; set; }
}

/// <summary>
/// Player status change event data
/// </summary>
public class PlayerStatusEventData
{
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Scoreboard update event data
/// </summary>
public class ScoreboardUpdateEventData
{
    public List<object> Scoreboard { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Game completion event data
/// </summary>
public class GameCompletionEventData
{
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<object> FinalResults { get; set; } = new();
}

/// <summary>
/// Player finished event data
/// </summary>
public class PlayerFinishedEventData
{
    public string Message { get; set; } = string.Empty;
    public int FinalScore { get; set; }
    public int TotalQuestions { get; set; }
}