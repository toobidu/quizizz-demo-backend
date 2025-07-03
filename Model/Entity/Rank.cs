namespace ConsoleApp1.Model.Entity;

public class Rank
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public User User { get; set; }
}