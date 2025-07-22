using ConsoleApp1.Model.DTO.Users;
namespace ConsoleApp1.Service.Interface;
public interface IRoleService
{
    Task<RoleDTO> GetRoleByIdAsync(int id);
    Task<List<RoleDTO>> GetAllRolesAsync();
    Task<RoleDTO> CreateRoleAsync(RoleDTO role);
    Task<RoleDTO> UpdateRoleAsync(RoleDTO role);
    Task DeleteRoleAsync(int id);
    Task<List<RoleDTO>> GetRolesByUserIdAsync(int userId);
    Task<bool> RoleNameExistsAsync(string roleName);
    Task<List<RoleDTO>> GetRolesByPermissionIdAsync(int permissionId);
    Task<List<RoleDTO>> GetRolesByUserIdAndPermissionNamesAsync(int userId, List<string> permissionNames);
}
