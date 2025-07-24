using System.Text.Json.Serialization;
namespace ConsoleApp1.Model.DTO.Rooms;
public class CreateRoomRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("isPrivate")]
    public bool IsPrivate { get; set; }
    [JsonPropertyName("settings")]
    public RoomSettings Settings { get; set; } = new();
    // Computed properties for backward compatibility
    public int MaxPlayers => Settings?.MaxPlayers ?? 0;
    public string GameMode => Settings?.GameMode ?? "battle";
    public int? TopicId => Settings?.TopicId ?? 1; // Lấy trực tiếp từ Settings
    public int? QuestionCount => Settings?.QuestionCount;
    public int? CountdownSeconds => Settings?.TimeLimit;
    public bool ValidField()
    {
        if (string.IsNullOrWhiteSpace(Name) || MaxPlayers <= 0)
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(GameMode) && Settings != null)
        {
            Settings.GameMode = MaxPlayers == 2 ? "1vs1" : "battle";
        }
        if (GameMode == "1vs1" && MaxPlayers != 2)
        {
            return false;
        }
        if (GameMode == "battle" && MaxPlayers < 2)
        {
            return false;
        }
        if (QuestionCount.HasValue && QuestionCount.Value <= 0)
        {
            return false;
        }
        if (CountdownSeconds.HasValue && CountdownSeconds.Value <= 0)
        {
            return false;
        }
        bool isValid = GameMode == "1vs1" || GameMode == "battle";
        return isValid;
    }
    public CreateRoomRequest() { }
    public CreateRoomRequest(string name, bool isPrivate, int maxPlayers, string gameMode = "battle")
    {
        Name = name;
        IsPrivate = isPrivate;
        Settings = new RoomSettings
        {
            MaxPlayers = maxPlayers,
            GameMode = gameMode
        };
    }
}
public class RoomSettings
{
    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; set; }
    [JsonPropertyName("timeLimit")]
    public int TimeLimit { get; set; }
    [JsonPropertyName("questionCount")]
    public int QuestionCount { get; set; }
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = string.Empty;
    [JsonPropertyName("topicId")]
    public int? TopicId { get; set; }
    [JsonPropertyName("gameMode")]
    public string GameMode { get; set; } = "battle";
}
