namespace ConsoleApp1.Service.Interface;
public interface IRolePermissionService
{
    Task AddPermissionToRoleAsync(int roleId, int permissionId);
    Task RemovePermissionFromRoleAsync(int roleId, int permissionId);
    Task AddPermissionsToRoleAsync(int roleId, List<int> permissionIds);
    Task RemovePermissionsFromRoleAsync(int roleId, List<int> permissionIds);
}
