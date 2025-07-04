namespace ConsoleApp1.Model.DTO;

public class RoomDTO
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int OwnerId{ get; set; }
    
    public RoomDTO(string code, string name, bool isPrivate, int ownerId) =>
        (Code, Name, IsPrivate, OwnerId) = (code, name, isPrivate, ownerId);
}