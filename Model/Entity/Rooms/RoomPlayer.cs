namespace ConsoleApp1.Model.Entity.Rooms;

public class RoomPlayer
{
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public RoomPlayer() { }

    public RoomPlayer(int roomId, int userId, int score, TimeSpan timeTaken, 
                     DateTime createdAt, DateTime updatedAt)
    {
        RoomId = roomId;
        UserId = userId;
        Score = score;
        TimeTaken = timeTaken;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}