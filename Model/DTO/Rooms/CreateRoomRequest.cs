namespace ConsoleApp1.Model.DTO;

public class CreateRoomRequest
{
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int MaxPlayers { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Name) && MaxPlayers > 0;
    }

    public CreateRoomRequest(string name, bool isPrivate, int maxPlayers) =>
        (Name, IsPrivate, MaxPlayers) = (name, isPrivate, maxPlayers);
}