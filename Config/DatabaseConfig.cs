namespace ConsoleApp1.Config;
public static class DatabaseConfig
{
    public static string GetConnectionString()
    {
        // Lấy từ appsettings hoặc environment variables
        return "Host=localhost;Database=quizizz;Username=postgres;Password=123456;";
    }
}
