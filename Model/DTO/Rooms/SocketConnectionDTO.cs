namespace ConsoleApp1.Model.DTO.Rooms;
public class SocketConnectionDTO
{
    public int Id { get; set; }
    public string SocketId { get; set; }
    public int? UserId { get; set; }
    public int? RoomId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
}
