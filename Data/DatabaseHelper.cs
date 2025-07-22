using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
namespace ConsoleApp1.Data;
public class DatabaseHelper
{
    private readonly string _connectionString;
    public DatabaseHelper(string connectionString)
    {
        _connectionString = connectionString;
    }
    public IDbConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
    public static string GetConnectionStringFromConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json");
        IConfiguration config = builder.Build();
        return config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }
}
