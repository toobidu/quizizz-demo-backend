namespace ConsoleApp1.Model.DTO;

public class RoomDTO
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId{ get; set; }
    public int MaxPlayers { get; set; }
    
    public RoomDTO(string code, string name, bool isPrivate, int ownerId, int maxPlayers) =>
        (Code, Name, IsPrivate, OwnerId, MaxPlayers) = (code, name, isPrivate, ownerId, maxPlayers);
}