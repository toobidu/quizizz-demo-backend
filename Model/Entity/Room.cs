namespace ConsoleApp1.Model.Entity;

public class Room
{
    public int Id { get; set; }
    public string RoomCode { get; set; }
    public string RomeName { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId { get; set; }
    
    public User Owner { get; set; }
    public ICollection<RoomPlayer> Players { get; set; }
    public ICollection<Question> Questions { get; set; }
}