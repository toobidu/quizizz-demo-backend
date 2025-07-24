namespace ConsoleApp1.Model.DTO.Rooms;

public class LeaderboardDTO
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Score { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalAnswers { get; set; }
    public int Rank { get; set; }
}