namespace ConsoleApp1.Model.Entity.Rooms;

public class Room
{
    public int Id { get; set; }
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId { get; set; }
    public string Status { get; set; }
    public int MaxPlayers { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Room() { }

    public Room(int id, string roomCode, string roomName, bool isPrivate, 
               int ownerId, string status, int maxPlayers, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        RoomCode = roomCode;
        RoomName = roomName;
        IsPrivate = isPrivate;
        OwnerId = ownerId;
        Status = status;
        MaxPlayers = maxPlayers;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}