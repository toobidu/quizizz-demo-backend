using System.Text.Json;

namespace ConsoleApp1.Config;

public class ConfigLoader
{
    public static ManageConfig Load()
    {
        var json = File.ReadAllText("appsettings.json");
        var root = JsonDocument.Parse(json).RootElement;

        var config = new ManageConfig
        {
            Security = JsonSerializer.Deserialize<SecurityConfig>(root.GetProperty("AppSettings").GetRawText())!,
            Redis = JsonSerializer.Deserialize<RedisConfig>(root.GetProperty("Redis").GetRawText())!,
            ConnectionStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(root.GetProperty("ConnectionStrings").GetRawText())!
        };

        return config;
    }
}
