namespace ConsoleApp1.Model.Entity;

public class UserAnswer
{
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public TimeSpan TimeTaken { get; set; }

    public UserAnswer()
    {
    }

    public UserAnswer(int userId, int roomId, int questionId, int answerId, bool isCorrect, TimeSpan timeTaken) =>
        (UserId, RoomId, QuestionId, AnswerId, IsCorrect, TimeTaken) =
        (userId, roomId, questionId, answerId, isCorrect, timeTaken);
}