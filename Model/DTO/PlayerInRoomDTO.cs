namespace ConsoleApp1.Model.DTO;

public class PlayerInRoomDTO
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public int Score { get; set; }
    public string TimeTaken { get; set; }

    public PlayerInRoomDTO(int userId, string username, int score, TimeSpan timeTaken)
    {
        UserId = userId;
        Username = username;
        Score = score;
        TimeTaken = $"{timeTaken.TotalSeconds:F2}s";
    }
}