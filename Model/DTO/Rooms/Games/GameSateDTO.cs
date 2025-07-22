namespace ConsoleApp1.Model.DTO.Rooms.Games;
public class GameStateDTO
{
    public string RoomCode { get; set; }
    public string Status { get; set; }
    public int CurrentQuestionIndex { get; set; }
    public int TotalQuestions { get; set; }
    public List<PlayerInRoomDTO> Players { get; set; }
    public DateTime? QuestionStartTime { get; set; }
    public GameStateDTO(string roomCode, string status, int currentQuestionIndex, 
        int totalQuestions, List<PlayerInRoomDTO> players, DateTime? questionStartTime)
    {
        RoomCode = roomCode;
        Status = status;
        CurrentQuestionIndex = currentQuestionIndex;
        TotalQuestions = totalQuestions;
        Players = players;
        QuestionStartTime = questionStartTime;
    }
}
