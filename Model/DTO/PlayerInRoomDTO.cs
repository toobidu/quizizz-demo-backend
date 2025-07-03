namespace ConsoleApp1.Model.DTO;

public class PlayerInRoomDTO
{
    public string Username { get; set; }
    public int Score { get; set; }
    public string TimeTaken { get; set; } 

    public PlayerInRoomDTO(string username, int score, TimeSpan timeTaken)
    {
        Username = username;
        Score = score;
        TimeTaken = $"{timeTaken.TotalSeconds:F2}s";
    }
}