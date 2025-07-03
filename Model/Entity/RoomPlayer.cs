namespace ConsoleApp1.Model.Entity;

public class RoomPlayer
{
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }
    
    public Room Room { get; set; }
    public User User { get; set; }
}