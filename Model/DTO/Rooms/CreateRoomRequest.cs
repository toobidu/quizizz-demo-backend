namespace ConsoleApp1.Model.DTO.Rooms;

public class CreateRoomRequest
{
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int MaxPlayers { get; set; }
    public string GameMode { get; set; } = "battle";
    public int? TopicId { get; set; }
    public int? QuestionCount { get; set; }
    public int? CountdownSeconds { get; set; }

    public bool ValidField()
    {
        if (string.IsNullOrWhiteSpace(Name) || MaxPlayers <= 0) return false;
        if (GameMode == "1vs1" && MaxPlayers != 2) return false;
        if (GameMode == "battle" && MaxPlayers < 3) return false;
        return GameMode == "1vs1" || GameMode == "battle";
    }

    public CreateRoomRequest(string name, bool isPrivate, int maxPlayers, string gameMode = "battle") =>
        (Name, IsPrivate, MaxPlayers, GameMode) = (name, isPrivate, maxPlayers, gameMode);
}