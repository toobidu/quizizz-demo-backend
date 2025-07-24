namespace ConsoleApp1.Model.DTO.Rooms;
public class PlayerInRoomDTO
{
    public int UserId { get; set; }
    public required string Username { get; set; }
    public int Score { get; set; }
    public TimeSpan TimeTaken { get; set; }
    public required string Status { get; set; }
    public required string SocketId { get; set; }
    public DateTime? LastActivity { get; set; }
}
