namespace ConsoleApp1.Model.DTO;

public class RoomSummaryDTO
{
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int PlayerCount { get; set; }
    
    public RoomSummaryDTO(string roomCode, string roomName, bool isPrivate, int playerCount) =>
        (RoomCode, RoomName, IsPrivate, PlayerCount) = (roomCode, roomName, isPrivate, playerCount);
}