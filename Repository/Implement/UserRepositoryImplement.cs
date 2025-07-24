using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class UserRepositoryImplement : IUserRepository
{
    private readonly DatabaseHelper _dbHelper;
    public UserRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    
    public async Task<User?> GetUserByIdAsync(int userId)
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
        WHERE id = @UserId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { UserId = userId });
    }
    
    public async Task<User?> GetUserByUsernameAsync(string username)
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
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { Username = username });
    }
    
    public async Task<User?> GetUserByEmailAsync(string email)
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
        WHERE LOWER(TRIM(email)) = LOWER(TRIM(@Email))";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<User>(query, new { Email = email });
    }
    
    public async Task<List<User>> GetAllUsersAsync(int page, int limit)
    {
        const string query = @"
        SELECT 
            id AS Id,
            username AS Username,
            full_name AS FullName,
            email AS Email,
            phone_number AS PhoneNumber,
            address AS Address,
            type_account AS TypeAccount,
            created_at AS CreatedAt,
            updated_at AS UpdatedAt
        FROM users
        ORDER BY id
        LIMIT @Limit OFFSET @Offset";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<User>(query, new { Limit = limit, Offset = (page - 1) * limit });
        return result.ToList();
    }
    
    public async Task<int> CreateUserAsync(User user)
    {
        const string query = @"
        INSERT INTO users (username, full_name, email, phone_number, address, password, type_account)
        VALUES (@Username, @FullName, @Email, @PhoneNumber, @Address, @Password, @TypeAccount)
        RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, user);
    }
    
    public async Task<bool> UpdateUserAsync(User user)
    {
        const string query = @"
        UPDATE users
        SET username = @Username,
            full_name = @FullName,
            email = @Email,
            phone_number = @PhoneNumber,
            address = @Address,
            password = @Password,
            type_account = @TypeAccount
        WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, user);
        return affected > 0;
    }
    
    public async Task<bool> DeleteUserAsync(int userId)
    {
        using var conn = CreateConnection();
        var npgsqlConn = (NpgsqlConnection)conn;
        await npgsqlConn.OpenAsync();
        using var transaction = npgsqlConn.BeginTransaction();
        try
        {
            // Delete user_roles first
            const string deleteUserRoles = @"DELETE FROM user_roles WHERE user_id = @UserId";
            await conn.ExecuteAsync(deleteUserRoles, new { UserId = userId }, transaction);
            // Then delete the user
            const string deleteUser = @"DELETE FROM users WHERE id = @UserId";
            var affectedRows = await conn.ExecuteAsync(deleteUser, new { UserId = userId }, transaction);
            await transaction.CommitAsync();
            return affectedRows > 0;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<User?> GetByIdAsync(int id)
    {
        return await GetUserByIdAsync(id);
    }
    
    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await GetUserByUsernameAsync(username);
    }
    
    public async Task<int> AddAsync(User user)
    {
        return await CreateUserAsync(user);
    }
    
    public async Task UpdateAsync(User user)
    {
        await UpdateUserAsync(user);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        return await DeleteUserAsync(id);
    }
    
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM users WHERE username = @Username)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { Username = username });
    }
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await GetUserByEmailAsync(email);
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
