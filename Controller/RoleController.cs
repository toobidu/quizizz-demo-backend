using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class RoleController
{
    private readonly IRoleService _roleService;
    private readonly IAuthorizationService _authService;
    private readonly JwtHelper _jwt;
    public RoleController(
        IRoleService roleService,
        IAuthorizationService authService,
        JwtHelper jwt)
    {
        _roleService = roleService;
        _authService = authService;
        _jwt = jwt;
    }
    private async Task<(bool isAuthorized, int userId)> IsAuthorized(string token)
    {
        int? userId = _jwt.GetUserIdFromToken(token);
        if (userId == null) return (false, -1);
        var hasPermission = await _authService.HasPermissionAsync(userId.Value, "ManageRoles");
        return (hasPermission, userId.Value);
    }
    /*
    GET /api/roles/{id}
    */
    public async Task<RoleDTO?> GetRoleByIdAsync(int id, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;
        return await _roleService.GetRoleByIdAsync(id);
    }
    /*
    GET /api/roles
    */
    public async Task<List<RoleDTO>> GetAllRolesAsync(string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<RoleDTO>();
        return await _roleService.GetAllRolesAsync();
    }
    /*
    POST /api/roles
    */
    public async Task<RoleDTO?> CreateRoleAsync(RoleDTO role, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;
        return await _roleService.CreateRoleAsync(role);
    }
    /*
    PUT /api/roles
    */
    public async Task<RoleDTO?> UpdateRoleAsync(RoleDTO role, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;
        return await _roleService.UpdateRoleAsync(role);
    }
    /*
    DELETE /api/roles/{id}
    */
    public async Task<string> DeleteRoleAsync(int id, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n không có quy?n th?c hi?n hành d?ng này.";
        await _roleService.DeleteRoleAsync(id);
        return "Xóa role thành công";
    }
    /*
    GET /api/roles/user/{userId}
    */
    public async Task<List<RoleDTO>> GetRolesByUserIdAsync(int userId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<RoleDTO>();
        return await _roleService.GetRolesByUserIdAsync(userId);
    }
    /*
    GET /api/roles/exists?name={roleName}
    */
    public async Task<bool> RoleNameExistsAsync(string roleName, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return false;
        return await _roleService.RoleNameExistsAsync(roleName);
    }
    /*
    GET /api/roles/by-permission/{permissionId}
    */
    public async Task<List<RoleDTO>> GetRolesByPermissionIdAsync(int permissionId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<RoleDTO>();
        return await _roleService.GetRolesByPermissionIdAsync(permissionId);
    }
    /*
    POST /api/roles/by-user-permissions
    */
    public async Task<List<RoleDTO>> GetRolesByUserIdAndPermissionNamesAsync(
        int userId,
        List<string> permissionNames,
        string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<RoleDTO>();
        return await _roleService.GetRolesByUserIdAndPermissionNamesAsync(userId, permissionNames);
    }
}
