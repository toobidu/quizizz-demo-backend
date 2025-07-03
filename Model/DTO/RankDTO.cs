namespace ConsoleApp1.Model.DTO;

public class RankDTO
{
    public string Username { get; set; }
    public int Score { get; set; }
    public string TimeTaken { get; set; }

    public RankDTO(string username, int score, TimeSpan timeTaken)
    {
        Username = username;
        Score = score;
        TimeTaken = $"{timeTaken.TotalSeconds:F2}s";
    }
}