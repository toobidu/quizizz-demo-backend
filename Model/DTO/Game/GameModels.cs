namespace ConsoleApp1.Model.DTO.Game;

public class GamePlayer
{
    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string SocketId { get; set; } = string.Empty;
    public int Score { get; set; } = 0;
    public string Status { get; set; } = "waiting"; // waiting, ready, answering, answered
    public bool IsHost { get; set; } = false;
}

public class GameRoom
{
    public string RoomCode { get; set; } = string.Empty;
    public List<GamePlayer> Players { get; set; } = new();
    public string GameState { get; set; } = "lobby"; // lobby, starting, question, result, finished
    public int CurrentQuestionIndex { get; set; } = 0;
    public int TotalQuestions { get; set; } = 0;
    public DateTime? QuestionStartTime { get; set; }
    public int QuestionTimeLimit { get; set; } = 30;
}

public class PlayerAnswer
{
    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; }
    public object Answer { get; set; } = new();
    public long Timestamp { get; set; }
    public int TimeToAnswer { get; set; } // seconds
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public int QuestionIndex { get; set; }
}

public class QuestionData
{
    public int QuestionId { get; set; }
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Type { get; set; } = "multiple_choice"; // multiple_choice, true_false
    public string Topic { get; set; } = string.Empty;
}

public class ScoreboardEntry
{
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int Rank { get; set; }
    public int CorrectAnswers { get; set; }
    public double AverageTime { get; set; }
}