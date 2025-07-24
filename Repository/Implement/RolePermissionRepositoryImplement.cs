using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class RolePermissionRepositoryImplement : IRolePermissionRepository
{
    private readonly DatabaseHelper _dbHelper;
    public RolePermissionRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    public async Task<RolePermission?> GetByRoleIdAndPermissionIdAsync(int roleId, int permissionId)
    {
        const string query = @"
            SELECT role_id AS RoleId, permission_id AS PermissionId
            FROM role_permissions
            WHERE role_id = @RoleId AND permission_id = @PermissionId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RolePermission>(
            query,
            new { RoleId = roleId, PermissionId = permissionId }
        );
    }
    public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId)
    {
        const string query = @"
            SELECT role_id AS RoleId, permission_id AS PermissionId
            FROM role_permissions
            WHERE role_id = @RoleId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RolePermission>(query, new { RoleId = roleId });
        return result.ToList();
    }
    public async Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(int permissionId)
    {
        const string query = @"
            SELECT role_id AS RoleId, permission_id AS PermissionId
            FROM role_permissions
            WHERE permission_id = @PermissionId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RolePermission>(query, new { PermissionId = permissionId });
        return result.ToList();
    }
    public async Task<int> AddAsync(RolePermission rolePermission)
    {
        const string query = @"
            INSERT INTO role_permissions (role_id, permission_id)
            VALUES (@RoleId, @PermissionId)
            RETURNING role_id"; // ho?c RETURNING permission_id n?u b?n mu?n l?y c�i n�o
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, rolePermission);
    }
    public async Task<bool> DeleteByRoleIdAsync(int roleId)
    {
        const string query = @"DELETE FROM role_permissions WHERE role_id = @RoleId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { RoleId = roleId });
        return affected > 0;
    }
    public async Task<bool> DeleteByRoleIdAndPermissionIdAsync(int roleId, int permissionId)
    {
        const string query = @"DELETE FROM role_permissions WHERE role_id = @RoleId AND permission_id = @PermissionId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { RoleId = roleId, PermissionId = permissionId });
        return affected > 0;
    }
}
