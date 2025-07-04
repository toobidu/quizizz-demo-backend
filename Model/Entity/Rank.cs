namespace ConsoleApp1.Model.Entity;

public class Rank
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Rank(int id, int userId, int totalScore, int gamesPlayed, DateTime updatedAt) =>
        (Id, UserId, TotalScore, GamesPlayed, UpdatedAt) = (id, userId, totalScore, gamesPlayed, updatedAt);
}