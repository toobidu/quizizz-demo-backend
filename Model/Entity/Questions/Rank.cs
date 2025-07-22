namespace ConsoleApp1.Model.Entity.Questions;
public class Rank
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Rank() { }
    public Rank(int id, int userId, int totalScore, int gamesPlayed, 
               DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        UserId = userId;
        TotalScore = totalScore;
        GamesPlayed = gamesPlayed;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
