using System.Text.Json.Serialization;
namespace ConsoleApp1.Model.DTO.Game;
/// <summary>
/// Format chuẩn cho câu hỏi gửi đến frontend
/// Đảm bảo camelCase format cho tất cả fields
/// </summary>
public class GameQuestionEventData
{
    [JsonPropertyName("questionId")]
    public int QuestionId { get; set; }
    [JsonPropertyName("questionIndex")]
    public int QuestionIndex { get; set; }
    [JsonPropertyName("questionText")]
    public string QuestionText { get; set; } = string.Empty;
    [JsonPropertyName("options")]
    public List<QuestionOptionData> Options { get; set; } = new();
    [JsonPropertyName("questionType")]
    public string QuestionType { get; set; } = "multiple_choice"; // multiple_choice, true_false, text_input
    [JsonPropertyName("timeLimit")]
    public int TimeLimit { get; set; } // seconds
    [JsonPropertyName("points")]
    public int Points { get; set; } = 10;
    [JsonPropertyName("totalQuestions")]
    public int TotalQuestions { get; set; }
    [JsonPropertyName("currentQuestion")]
    public int CurrentQuestion { get; set; }
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "medium"; // easy, medium, hard
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }
}
/// <summary>
/// Option data cho câu hỏi
/// </summary>
public class QuestionOptionData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
    [JsonPropertyName("optionIndex")]
    public int OptionIndex { get; set; }
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; } = false; // Chỉ show khi reveal answer
}
/// <summary>
/// Format chuẩn cho câu trả lời từ frontend
/// </summary>
public class PlayerAnswerSubmission
{
    [JsonPropertyName("questionId")]
    public int QuestionId { get; set; }
    [JsonPropertyName("questionIndex")]
    public int QuestionIndex { get; set; }
    [JsonPropertyName("selectedOptionId")]
    public int SelectedOptionId { get; set; }
    [JsonPropertyName("selectedOptionIndex")]
    public int SelectedOptionIndex { get; set; }
    [JsonPropertyName("textAnswer")]
    public string? TextAnswer { get; set; } // For text input questions
    [JsonPropertyName("timeToAnswer")]
    public int TimeToAnswer { get; set; } // milliseconds
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
    [JsonPropertyName("roomCode")]
    public string RoomCode { get; set; } = string.Empty;
}
/// <summary>
/// Kết quả câu trả lời gửi về cho player
/// </summary>
public class AnswerResultData
{
    [JsonPropertyName("questionId")]
    public int QuestionId { get; set; }
    [JsonPropertyName("questionIndex")]
    public int QuestionIndex { get; set; }
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }
    [JsonPropertyName("correctOptionId")]
    public int CorrectOptionId { get; set; }
    [JsonPropertyName("correctOptionIndex")]
    public int CorrectOptionIndex { get; set; }
    [JsonPropertyName("correctAnswerText")]
    public string CorrectAnswerText { get; set; } = string.Empty;
    [JsonPropertyName("pointsEarned")]
    public int PointsEarned { get; set; }
    [JsonPropertyName("totalPoints")]
    public int TotalPoints { get; set; }
    [JsonPropertyName("timeToAnswer")]
    public int TimeToAnswer { get; set; }
    [JsonPropertyName("rank")]
    public int Rank { get; set; }
    [JsonPropertyName("totalPlayers")]
    public int TotalPlayers { get; set; }
    [JsonPropertyName("explanation")]
    public string? Explanation { get; set; }
}
/// <summary>
/// Game progress data
/// </summary>
public class GameProgressData
{
    [JsonPropertyName("currentQuestionIndex")]
    public int CurrentQuestionIndex { get; set; }
    [JsonPropertyName("totalQuestions")]
    public int TotalQuestions { get; set; }
    [JsonPropertyName("gameProgress")]
    public double GameProgress { get; set; } // percentage 0-100
    [JsonPropertyName("timeRemaining")]
    public int TimeRemaining { get; set; } // seconds
    [JsonPropertyName("gameState")]
    public string GameState { get; set; } = string.Empty; // lobby, playing, paused, finished
    [JsonPropertyName("playersAnswered")]
    public int PlayersAnswered { get; set; }
    [JsonPropertyName("totalPlayers")]
    public int TotalPlayers { get; set; }
}
