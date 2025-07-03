namespace ConsoleApp1.Model.DTO;

public class CreateRoomRequest
{
    public string Name { get; set; }
    public bool IsPrivate { get; set; }

    public CreateRoomRequest(string name, bool isPrivate)
    {
        Name = name;
        IsPrivate = isPrivate;
    }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }
}