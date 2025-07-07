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

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<User?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM users WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { Id = id });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string query = @"
        SELECT 
            id AS Id,
            username AS Username,
            password AS Password,
            type_account AS TypeAccount
        FROM users 
        WHERE username = @Username";
        using var conn = CreateConnection();
        var user = await conn.QuerySingleOrDefaultAsync<User>(query, new { Username = username });
        Console.WriteLine($"Database query result for username {username}: {user != null}");
        if (user != null)
        {
            Console.WriteLine($"User details - Id: {user.Id}, Username: {user.Username}, TypeAccount: {user.TypeAccount}");
        }
        return user;
    }

    public async Task<int> AddAsync(User user)
    {
        const string query = @"
            INSERT INTO users (username, password, type_account)
            VALUES (@Username, @Password, @TypeAccount)
            RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, user);
    }

    public async Task UpdateAsync(User user)
    {
        const string query = @"
            UPDATE users
            SET username = @Username,
                password = @Password,
                type_account = @TypeAccount
            WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, user);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM users WHERE id = @Id";
        using var conn = CreateConnection();
        var affectedRows = await conn.ExecuteAsync(query, new { Id = id });
        return affectedRows > 0;
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM users WHERE username = @Username)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { Username = username });
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string query = @"SELECT id, username, password, type_account FROM users";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<User>(query);
        return result.ToList();
    }
}
