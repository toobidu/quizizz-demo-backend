namespace ConsoleApp1.Model.Entity;

public class UserAnswer
{
    public int UserId { get; set; }
    public int RoomId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public TimeSpan TimeTaken { get; set; }
}