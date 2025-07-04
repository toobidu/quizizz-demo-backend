namespace ConsoleApp1.Model.Entity;

public class Room
{
    public int Id { get; set; }
    public string RoomCode { get; set; }
    public string RoomName { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId { get; set; }
    
    public Room(int id, string roomCode, string roomName, bool isPrivate, int ownerId) =>
        (Id, RoomCode, RoomName, IsPrivate, OwnerId) = (id, roomCode, roomName, isPrivate, ownerId);
}