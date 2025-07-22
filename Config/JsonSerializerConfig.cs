using System.Text.Json;
using System.Text.Json.Serialization;
namespace ConsoleApp1.Config;
/// <summary>
/// Cấu hình JsonSerializer toàn cục cho ứng dụng
/// Chuẩn hóa về camelCase cho tất cả API responses và WebSocket messages
/// </summary>
public static class JsonSerializerConfig
{
    /// <summary>
    /// Cấu hình JsonSerializerOptions toàn cục với camelCase
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    /// <summary>
    /// Cấu hình JsonSerializerOptions cho WebSocket messages
    /// Đảm bảo format nhất quán với API responses
    /// </summary>
    public static readonly JsonSerializerOptions WebSocketOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    /// <summary>
    /// Serialize object với cấu hình camelCase
    /// </summary>
    public static string SerializeCamelCase(object obj)
    {
        return JsonSerializer.Serialize(obj, DefaultOptions);
    }
    /// <summary>
    /// Deserialize object với cấu hình camelCase
    /// </summary>
    public static T? DeserializeCamelCase<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }
}
