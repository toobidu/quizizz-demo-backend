namespace ConsoleApp1.Model.DTO.Rooms;
public class PlayerInRoomDTO
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public string Status { get; set; }
    public string SocketId { get; set; }
    public DateTime? LastActivity { get; set; }
    public PlayerInRoomDTO(int userId, string username, int score, TimeSpan timeTaken, 
                          string status, string socketId, DateTime? lastActivity) =>
        (UserId, Username, Score, TimeTaken, Status, SocketId, LastActivity) = 
        (userId, username, score, timeTaken, status, socketId, lastActivity);
    public PlayerInRoomDTO(int userId, string username, int score, TimeSpan timeTaken) =>
        (UserId, Username, Score, TimeTaken) = (userId, username, score, timeTaken);
}
