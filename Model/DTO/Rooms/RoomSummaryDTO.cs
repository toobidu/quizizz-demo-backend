namespace ConsoleApp1.Model.DTO.Rooms;

public class RoomSummaryDTO
{
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; }
    public string? TopicName { get; set; }
    public int QuestionCount { get; set; }
    public int CountdownTime { get; set; }
    public bool CanJoin { get; set; }
    
    public RoomSummaryDTO(string roomCode, string roomName, bool isPrivate, int playerCount, int maxPlayers, 
                         string status, string? topicName = null, int questionCount = 0, int countdownTime = 0)
    {
        RoomCode = roomCode;
        RoomName = roomName;
        IsPrivate = isPrivate;
        PlayerCount = playerCount;
        MaxPlayers = maxPlayers;
        Status = status;
        TopicName = topicName;
        QuestionCount = questionCount;
        CountdownTime = countdownTime;
        CanJoin = status == "waiting" && playerCount < maxPlayers;
    }
}