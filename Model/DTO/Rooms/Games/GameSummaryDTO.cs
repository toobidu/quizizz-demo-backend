namespace ConsoleApp1.Model.DTO.Rooms.Games;

public class GameSummaryDTO
{
    public int SessionId { get; set; }
    public int RoomId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int TotalQuestions { get; set; }
    public List<LeaderboardDTO> Leaderboard { get; set; } = new List<LeaderboardDTO>();
    public object Stats { get; set; } = new object();
}