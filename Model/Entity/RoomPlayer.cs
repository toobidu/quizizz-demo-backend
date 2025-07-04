namespace ConsoleApp1.Model.Entity;

public class RoomPlayer
{
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }
    
    public RoomPlayer(int roomId, int userId, int score, TimeSpan timeTaken) =>
        (RoomId, UserId, Score, TimeTaken) = (roomId, userId, score, timeTaken);
}