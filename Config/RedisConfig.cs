namespace ConsoleApp1.Config;
public class RedisConfig
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int DatabaseId { get; set; }
}
