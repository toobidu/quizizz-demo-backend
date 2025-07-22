using System.Text.Json.Serialization;
namespace ConsoleApp1.Model.DTO.WebSocket;
/// <summary>
/// Base WebSocket message format chuẩn hóa
/// Đảm bảo format nhất quán cho tất cả WebSocket events
/// </summary>
public class WebSocketMessage<T>
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public WebSocketMessage() { }
    public WebSocketMessage(string type, T data)
    {
        Type = type;
        Data = data;
        Timestamp = DateTime.UtcNow;
    }
}
/// <summary>
/// Player information trong WebSocket events
/// Chuẩn hóa tất cả về camelCase
/// </summary>
public class PlayerInfo
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("isHost")]
    public bool IsHost { get; set; }
    [JsonPropertyName("isReady")]
    public bool IsReady { get; set; }
    [JsonPropertyName("score")]
    public int Score { get; set; }
    [JsonPropertyName("joinTime")]
    public DateTime? JoinTime { get; set; }
}
/// <summary>
/// Player joined event data
/// </summary>
public class PlayerJoinedData
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("roomCode")]
    public string RoomCode { get; set; } = string.Empty;
    [JsonPropertyName("isHost")]
    public bool IsHost { get; set; }
    [JsonPropertyName("joinTime")]
    public DateTime JoinTime { get; set; } = DateTime.UtcNow;
}
/// <summary>
/// Player left event data
/// </summary>
public class PlayerLeftData
{
    [JsonPropertyName("userId")]
    public int UserId { get; set; }
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("roomCode")]
    public string RoomCode { get; set; } = string.Empty;
}
/// <summary>
/// Room players updated event data
/// </summary>
public class RoomPlayersUpdatedData
{
    [JsonPropertyName("roomCode")]
    public string RoomCode { get; set; } = string.Empty;
    [JsonPropertyName("players")]
    public List<PlayerInfo> Players { get; set; } = new();
    [JsonPropertyName("totalPlayers")]
    public int TotalPlayers { get; set; }
    [JsonPropertyName("maxPlayers")]
    public int MaxPlayers { get; set; } = 10;
    [JsonPropertyName("host")]
    public PlayerInfo? Host { get; set; }
}
/// <summary>
/// Host changed event data
/// </summary>
public class HostChangedData
{
    [JsonPropertyName("roomCode")]
    public string RoomCode { get; set; } = string.Empty;
    [JsonPropertyName("newHost")]
    public PlayerInfo NewHost { get; set; } = new();
    [JsonPropertyName("oldHost")]
    public PlayerInfo? OldHost { get; set; }
}
