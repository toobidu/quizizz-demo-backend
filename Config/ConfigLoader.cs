using System.Text.Json;

namespace ConsoleApp1.Config;

public class ConfigLoader
{
    public static ManageConfig Load()
    {
        var json = File.ReadAllText("appsettings.json");
        var root = JsonDocument.Parse(json).RootElement;

        var appSettings = root.GetProperty("AppSettings");

        var config = new ManageConfig()
        {
            Security = JsonSerializer.Deserialize<SecurityConfig>(appSettings.GetRawText())!,
            Redis = JsonSerializer.Deserialize<RedisConfig>(root.GetProperty("Redis").GetRawText())!
        };

        return config;
    }
}