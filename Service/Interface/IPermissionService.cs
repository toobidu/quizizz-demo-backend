using ConsoleApp1.Model.DTO.Users;
namespace ConsoleApp1.Service.Interface;
public interface IPermissionService
{
    Task<List<PermissionDTO>> GetAllPermissionsAsync();
    Task<PermissionDTO?> GetPermissionByIdAsync(int id);
    Task<bool> PermissionNameExistsAsync(string permissionName);
    Task<bool> AddPermissionAsync(PermissionDTO permissionDto);
    Task<bool> UpdatePermissionAsync(PermissionDTO permissionDto);
    Task<bool> DeletePermissionAsync(int id);
}
