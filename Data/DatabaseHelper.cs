using Microsoft.Extensions.Configuration;
using Npgsql;

namespace ConsoleApp1.Data;

public static class DatabaseHelper
{
    private static string GetConnectionString()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json");

        IConfiguration config = builder.Build();

        return config.GetConnectionString("DefaultConnection");
    }

    public static NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(GetConnectionString());
    }
}