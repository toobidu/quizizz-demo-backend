namespace ConsoleApp1.Config;

public class ManageConfig
{
    /*Cấu hình chung */
    public SecurityConfig Security { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
}