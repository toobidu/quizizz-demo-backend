using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

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

    public static SqlConnection GetConnection()
    {
        return new SqlConnection(GetConnectionString());
    }
}