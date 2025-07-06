using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByRoleIdAndPermissionIdAsync(int roleId, int permissionId);
    Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId);
    Task<IEnumerable<RolePermission>> GetByPermissionIdAsync(int permissionId);
    Task<int> AddAsync(RolePermission rolePermission);
    Task<bool> DeleteByRoleIdAsync(int roleId);
    Task<bool> DeleteByRoleIdAndPermissionIdAsync(int roleId, int permissionId);
}