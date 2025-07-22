using System.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class RoleRepositoryImplement : IRoleRepository
{
    public readonly string ConnectionString;
    public RoleRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }
    /*
    T?o Connection
    */
    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);
    public async Task<Role?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM roles WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Role>(query, new { Id = id });
    }
    public async Task<Role?> GetByRoleNameAsync(string roleName)
    {
        const string query = @"
            SELECT 
                id AS Id,
                role_name AS RoleName
            FROM roles 
            WHERE UPPER(role_name) = UPPER(@RoleName)";
        using var conn = CreateConnection();
        var role = await conn.QuerySingleOrDefaultAsync<Role>(query, new { RoleName = roleName });
        return role;
    }
    public async Task<int> AddAsync(Role role)
    {
        const string query = @"INSERT INTO roles (role_name) VALUES (@RoleName) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, role);
    }
    public async Task UpdateAsync(Role role)
    {
        const string query = @"UPDATE roles SET role_name = @RoleName WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, role);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM roles WHERE id = @Id";
        using var conn = CreateConnection();
        var affectedRows = await conn.ExecuteAsync(query, new { Id = id });
        return affectedRows > 0;
    }
    public async Task<bool> ExistsByRoleNameAsync(string roleName)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM roles WHERE role_name = @RoleName)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { RoleName = roleName });
    }
    public async Task<IEnumerable<Role>> GetAllAsync()
    {
        const string query = @"SELECT id AS Id, role_name AS RoleName FROM roles";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Role>(query);
        return result.ToList();
    }
}
