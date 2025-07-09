using ConsoleApp1.Model.DTO.Questions;

namespace ConsoleApp1.Model.DTO.Rooms.Games;

public class GameSummaryDTO
{
    public string RoomCode { get; set; }
    public DateTime GameDate { get; set; }
    public int TotalQuestions { get; set; }
    public List<PlayerProgressDTO> FinalResults { get; set; }
    public List<QuestionStatisticsDTO> QuestionStats { get; set; }
    public TimeSpan TotalGameTime { get; set; }
    
    public GameSummaryDTO(string roomCode, DateTime gameDate, int totalQuestions,
        List<PlayerProgressDTO> finalResults, List<QuestionStatisticsDTO> questionStats,
        TimeSpan totalGameTime)
    {
        RoomCode = roomCode;
        GameDate = gameDate;
        TotalQuestions = totalQuestions;
        FinalResults = finalResults;
        QuestionStats = questionStats;
        TotalGameTime = totalGameTime;
    }
}