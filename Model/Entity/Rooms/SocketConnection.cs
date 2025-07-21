namespace ConsoleApp1.Model.Entity.Rooms;

using ConsoleApp1.Model.Entity.Users;

public class SocketConnection
{
    public int Id { get; set; }
    public string SocketId { get; set; }
    public int? UserId { get; set; }
    public int? RoomId { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActivity { get; set; }
    
    // Navigation properties
    public User User { get; set; }
    public Room Room { get; set; }
}