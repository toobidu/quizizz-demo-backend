namespace ConsoleApp1.Model.DTO.Rooms;
public class JoinRoomRequest
{
    public string RoomCode { get; set; }
    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(RoomCode);
    }
    public JoinRoomRequest(string roomCode) =>
        RoomCode = roomCode;   
}
