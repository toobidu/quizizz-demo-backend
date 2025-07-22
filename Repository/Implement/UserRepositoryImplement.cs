using System.Data;
using ConsoleApp1.Model.Entity.Users;
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
        const string query = @"
        SELECT 
            id AS Id,
            username AS Username,
            full_name AS FullName,
            email AS Email,
            phone_number AS PhoneNumber,
            address AS Address,
            password AS Password,
            type_account AS TypeAccount,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM users 
        WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { Id = id });
    }
    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string query = @"
        SELECT 
            id AS Id,
            username AS Username,
            full_name AS FullName,
            email AS Email,
            phone_number AS PhoneNumber,
            address AS Address,
            password AS Password,
            type_account AS TypeAccount,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM users 
        WHERE username = @Username";
        using var conn = CreateConnection();
        var user = await conn.QuerySingleOrDefaultAsync<User>(query, new { Username = username });
        if (user != null)
        {
        }
        return user;
    }
    public async Task<int> AddAsync(User user)
    {
        const string query = @"
        INSERT INTO users (username, full_name, email, phone_number, address, password, type_account)
        VALUES (@Username, @FullName, @Email, @PhoneNumber, @Address, @Password, @TypeAccount)
        RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, user);
    }
    public async Task UpdateAsync(User user)
    {
        const string query = @"
        UPDATE users
        SET username = @Username,
            full_name = @FullName,
            email = @Email,
            phone_number = @PhoneNumber,
            address = @Address,
            password = @Password,
            type_account = @TypeAccount,
            updated_at = @UpdatedAt
        WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, user);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        using var conn = CreateConnection();
        var npgsqlConn = (NpgsqlConnection)conn;
        await npgsqlConn.OpenAsync();
        using var transaction = npgsqlConn.BeginTransaction();
        try
        {
            // Delete user_roles first
            const string deleteUserRoles = @"DELETE FROM user_roles WHERE user_id = @Id";
            await conn.ExecuteAsync(deleteUserRoles, new { Id = id }, transaction);
            // Then delete the user
            const string deleteUser = @"DELETE FROM users WHERE id = @Id";
            var affectedRows = await conn.ExecuteAsync(deleteUser, new { Id = id }, transaction);
            await transaction.CommitAsync();
            return affectedRows > 0;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM users WHERE username = @Username)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { Username = username });
    }
    public async Task<User?> GetByEmailAsync(string email)
    {
        const string query = @"
        SELECT 
            id AS Id,
            username AS Username,
            full_name AS FullName,
            email AS Email,
            phone_number AS PhoneNumber,
            address AS Address,
            password AS Password,
            type_account AS TypeAccount
        FROM users 
        WHERE LOWER(TRIM(email)) = LOWER(TRIM(@Email))";
        using var conn = CreateConnection();
        var user = await conn.QuerySingleOrDefaultAsync<User>(query, new { Email = email });
        if (user != null)
        {
        }
        return user;
    }
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        const string query = @"SELECT id, username, password, type_account FROM users";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<User>(query);
        return result.ToList();
    }
    public async Task<User?> GetByPhoneNumberAsync(string phoneNumber)
    {
        const string query = @"SELECT id, phone_number FROM users WHERE phone_number = @PhoneNumber";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { PhoneNumber = phoneNumber });
    }
}
