namespace ConsoleApp1.Model.DTO;

public class RoomDTO
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public string OwnerUsername { get; set; }

    public RoomDTO(string code, string name, bool isPrivate, string ownerUsername)
    {
        Code = code;
        Name = name;
        IsPrivate = isPrivate;
        OwnerUsername = ownerUsername;
    }
}