namespace ConsoleApp1.Model.Entity.Rooms;
public class GameSession
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public required string GameState { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TimeLimit { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    // Navigation properties
    public Room? Room { get; set; }
}
