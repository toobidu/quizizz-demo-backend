namespace ConsoleApp1.Config;
public class ManageConfig
{
    /*C?u h�nh chung */
    public SecurityConfig Security { get; set; } = new();
    public RedisConfig Redis { get; set; } = new();
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
}
