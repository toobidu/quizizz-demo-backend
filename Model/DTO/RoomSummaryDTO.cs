namespace ConsoleApp1.Model.DTO;

public class RoomSummaryDTO
{
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int PlayerCount { get; set; }
}