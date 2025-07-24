using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class PermissionRepositoryImplement : IPermissionRepository
{
    private readonly DatabaseHelper _dbHelper;
    public PermissionRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    public async Task<Permission?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM permissions WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Permission>(query, new { Id = id });
    }
    public async Task<Permission?> GetByPermissionNameAsync(string permissionName)
    {
        const string query = @"SELECT id, permission_name FROM permissions WHERE permission_name = @PermissionName";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Permission>(query, new { PermissionName = permissionName });
    }
    public async Task<IEnumerable<Permission>> GetAllAsync()
    {
        const string query = @"SELECT id, permission_name FROM permissions";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Permission>(query);
        return result.ToList();
    }
    public async Task<int> AddAsync(Permission permission)
    {
        const string query = @"INSERT INTO permissions (permission_name) VALUES (@PermissionName) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, permission);
    }
    public async Task UpdateAsync(Permission permission)
    {
        const string query = @"UPDATE permissions SET permission_name = @PermissionName WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, permission);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM permissions WHERE id = @Id";
        using var conn = CreateConnection();
        var affectedRows = await conn.ExecuteAsync(query, new { Id = id });
        return affectedRows > 0;
    }
    public async Task<bool> ExistsByPermissionNameAsync(string permissionName)
    {
        const string query = @"SELECT EXISTS (SELECT 1 FROM permissions WHERE permission_name = @PermissionName)";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { PermissionName = permissionName });
    }
    public async Task<IEnumerable<string>> GetPermissionsByUserIdAsync(int userId)
    {
        const string query = @"
        SELECT p.permission_name
        FROM permissions p
        INNER JOIN role_permissions rp ON p.id = rp.permission_id
        INNER JOIN user_roles ur ON ur.role_id = rp.role_id
        WHERE ur.user_id = @UserId
    ";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<string>(query, new { UserId = userId });
        return result.Distinct().ToList();
    }
    public async Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<int> ids)
    {
        const string query = @"SELECT id AS Id, permission_name AS PermissionName FROM permissions WHERE id = ANY(@Ids)";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Permission>(query, new { Ids = ids });
    }
}
