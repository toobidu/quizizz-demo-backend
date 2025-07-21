using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Model.Entity.Users;

public class UserAnswer
{
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public int? GameSessionId { get; set; }
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Question? Question { get; set; }
    public GameSession? GameSession { get; set; }

    public UserAnswer() { }

    public UserAnswer(int userId, int roomId, int questionId, int answerId, 
                     bool isCorrect, TimeSpan timeTaken, int? gameSessionId, int score,
                     DateTime createdAt, DateTime updatedAt)
    {
        UserId = userId;
        RoomId = roomId;
        QuestionId = questionId;
        AnswerId = answerId;
        IsCorrect = isCorrect;
        TimeTaken = timeTaken;
        GameSessionId = gameSessionId;
        Score = score;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
    
    public UserAnswer(int userId, int roomId, int questionId, int answerId, 
                     bool isCorrect, TimeSpan timeTaken, DateTime createdAt, DateTime updatedAt)
    {
        UserId = userId;
        RoomId = roomId;
        QuestionId = questionId;
        AnswerId = answerId;
        IsCorrect = isCorrect;
        TimeTaken = timeTaken;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}