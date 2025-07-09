namespace ConsoleApp1.Model.DTO.Rooms;

public class RoomSummaryDTO
{
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int PlayerCount { get; set; }
    public string Status { get; set; }
    
    public RoomSummaryDTO(string roomCode, string roomName, bool isPrivate, int playerCount, string status) =>
        (RoomCode, RoomName, IsPrivate, PlayerCount, Status) = 
        (roomCode, roomName, isPrivate, playerCount, status);
}