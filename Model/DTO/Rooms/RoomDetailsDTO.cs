namespace ConsoleApp1.Model.DTO.Rooms;

public class RoomDetailsDTO
{
    public int Id { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public int OwnerId { get; set; }
    public string OwnerUsername { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int MaxPlayers { get; set; }
    public int CurrentPlayerCount { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int CountdownTime { get; set; }
    public string GameMode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<PlayerInRoomDTO> Players { get; set; } = new();
}