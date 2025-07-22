namespace ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Model.Entity.Questions;
public class GameQuestion
{
    public int GameSessionId { get; set; }
    public int QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int TimeLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation properties
    public GameSession GameSession { get; set; }
    public Question Question { get; set; }
}
