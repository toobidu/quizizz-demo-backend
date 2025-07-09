namespace ConsoleApp1.Model.DTO;

public class PlayerInRoomDTO
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }

    public PlayerInRoomDTO(int userId, string username, int score, TimeSpan timeTaken) =>
        (UserId, Username, Score, TimeTaken) = (userId, username, score, timeTaken);
}