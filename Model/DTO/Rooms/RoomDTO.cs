namespace ConsoleApp1.Model.DTO.Rooms;

public class RoomDTO
{
    public int Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId { get; set; }
    public int MaxPlayers { get; set; }
    public string Status { get; set; }
    
    public RoomDTO(int id, string code, string name, bool isPrivate, int ownerId, int maxPlayers, string status) =>
        (Id, Code, Name, IsPrivate, OwnerId, MaxPlayers, Status) = 
        (id, code, name, isPrivate, ownerId, maxPlayers, status);
        
    public RoomDTO(string code, string name, bool isPrivate, int ownerId, int maxPlayers, string status) =>
        (Code, Name, IsPrivate, OwnerId, MaxPlayers, Status) = 
        (code, name, isPrivate, ownerId, maxPlayers, status);
}