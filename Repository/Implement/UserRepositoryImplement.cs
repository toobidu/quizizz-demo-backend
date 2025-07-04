using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class UserRepositoryImplement : IUserRepository
{
    public readonly string ConnectionString;

    public UserRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /*
    Tạo Connection
    */
    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<User?> GetByIdAsync(int id)
    {
        const string query = @"SELECT id, username, type_account FROM ""User"" WHERE id = @id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string query = @"SELECT id, username, type_account FROM ""User"" WHERE username = @username";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { username });
    }

    public async Task<int> AddAsync(User user)
    {
        const string query = @"INSERT INTO ""User"" (username, password, type_account) VALUES (@username, @password, @typeAccount) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, user);
    }

    public async Task UpdateAsync(User user)
    {
        const string query = @"UPDATE ""User"" SET username = @Username, password = @Password, type_account = @TypeAccount WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, user);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM ""User"" WHERE username = @username)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { username });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string query = @"SELECT id, username, password, type_account FROM ""User""";
        using var conn = CreateConnection();
        return await conn.QueryAsync<User>(query);
    }
}