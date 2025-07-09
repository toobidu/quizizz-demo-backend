namespace ConsoleApp1.Model.DTO;

public class RankDTO
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public int TotalScore { get; set; }
    public int GamesPlayed { get; set; }
    public string UpdatedAt { get; set; }
    
    public RankDTO(int userId, string username, int totalScore, int gamesPlayed, string updatedAt) =>
        (UserId, Username, TotalScore, GamesPlayed, UpdatedAt) = (userId, username, totalScore, gamesPlayed, updatedAt);
}