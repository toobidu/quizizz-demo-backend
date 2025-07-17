using ConsoleApp1.Model.DTO.Game;

namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Models cho Game Flow Service
/// </summary>

/// <summary>
/// Class nội bộ để quản lý session của một game
/// </summary>
public class GameSession
{
    public string RoomCode { get; set; } = string.Empty;
    public List<QuestionData> Questions { get; set; } = new();
    public int CurrentQuestionIndex { get; set; } = 0;
    public int GameTimeLimit { get; set; } = 300; // 5 phút mặc định
    public DateTime GameStartTime { get; set; }
    public bool IsGameActive { get; set; } = false;
    public bool IsGameEnded { get; set; } = false;
    public Timer? GameTimer { get; set; }
    public Timer? CountdownTimer { get; set; }
    public Dictionary<string, PlayerGameProgress> PlayerProgress { get; set; } = new();
}

/// <summary>
/// Class theo dõi tiến độ của từng người chơi
/// </summary>
public class PlayerGameProgress
{
    public string Username { get; set; } = string.Empty;
    public int CurrentQuestionIndex { get; set; } = 0;
    public int Score { get; set; } = 0;
    public List<PlayerAnswer> Answers { get; set; } = new();
    public bool HasFinished { get; set; } = false;
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Game start event data
/// </summary>
public class GameStartEventData
{
    public string Message { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public int? TimeLimit { get; set; }
    public int? TotalQuestions { get; set; }
}

/// <summary>
/// Question event data
/// </summary>
public class QuestionEventData
{
    public object Question { get; set; } = new();
    public int QuestionIndex { get; set; }
    public int TotalQuestions { get; set; }
    public int TimeRemaining { get; set; }
    public string GameState { get; set; } = string.Empty;
}

/// <summary>
/// Timer update event data
/// </summary>
public class TimerUpdateEventData
{
    public int TimeRemaining { get; set; }
    public int TotalTime { get; set; }
    public string GameState { get; set; } = string.Empty;
}

/// <summary>
/// Player progress event data
/// </summary>
public class PlayerProgressEventData
{
    public int CurrentQuestionIndex { get; set; }
    public int TotalQuestions { get; set; }
    public int Score { get; set; }
    public int AnswersCount { get; set; }
    public bool HasFinished { get; set; }
    public int TimeRemaining { get; set; }
}

/// <summary>
/// Progress update broadcast data
/// </summary>
public class ProgressUpdateEventData
{
    public List<object> Players { get; set; } = new();
    public string GameState { get; set; } = string.Empty;
    public int TimeRemaining { get; set; }
}

/// <summary>
/// Countdown event data
/// </summary>
public class CountdownEventData
{
    public int Count { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Game end event data
/// </summary>
public class GameEndEventData
{
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<object> FinalResults { get; set; } = new();
}