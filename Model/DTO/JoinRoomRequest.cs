namespace ConsoleApp1.Model.DTO;

public class JoinRoomRequest
{
    public string RoomCode { get; set; }

    public JoinRoomRequest(string roomCode)
    {
        RoomCode = roomCode;
    }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(RoomCode);
    }
}