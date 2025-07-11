using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO.Rooms;

public class CreateRoomRequest
{
    [JsonPropertyName("roomName")]
    public string Name { get; set; }
    public bool IsPrivate { get; set; }
    public int MaxPlayers { get; set; }
    public string GameMode { get; set; } = "battle";
    public int? TopicId { get; set; }
    public int? QuestionCount { get; set; }
    public int? CountdownSeconds { get; set; }

    public bool ValidField()
    {
        Console.WriteLine($"[VALIDATION] Name: '{Name}', MaxPlayers: {MaxPlayers}, GameMode: '{GameMode}', TopicId: {TopicId}, QuestionCount: {QuestionCount}, CountdownSeconds: {CountdownSeconds}");
        
        if (string.IsNullOrWhiteSpace(Name) || MaxPlayers <= 0)
        {
            Console.WriteLine($"[VALIDATION] Failed: Name empty or MaxPlayers <= 0");
            return false;
        }
        
        if (string.IsNullOrWhiteSpace(GameMode))
        {
            GameMode = MaxPlayers == 2 ? "1vs1" : "battle";
        }
        
        if (GameMode == "1vs1" && MaxPlayers != 2)
        {
            Console.WriteLine($"[VALIDATION] Failed: 1vs1 mode requires exactly 2 players");
            return false;
        }
        
        if (GameMode == "battle" && MaxPlayers < 2)
        {
            Console.WriteLine($"[VALIDATION] Failed: battle mode requires at least 2 players");
            return false;
        }
        
        if (QuestionCount.HasValue && QuestionCount.Value <= 0)
        {
            Console.WriteLine($"[VALIDATION] Failed: QuestionCount must be positive");
            return false;
        }
        
        if (CountdownSeconds.HasValue && CountdownSeconds.Value <= 0)
        {
            Console.WriteLine($"[VALIDATION] Failed: CountdownSeconds must be positive");
            return false;
        }
        
        bool isValid = GameMode == "1vs1" || GameMode == "battle";
        Console.WriteLine($"[VALIDATION] Result: {isValid}");
        return isValid;
    }

    public CreateRoomRequest(string name, bool isPrivate, int maxPlayers, string gameMode = "battle") =>
        (Name, IsPrivate, MaxPlayers, GameMode) = (name, isPrivate, maxPlayers, gameMode);
}