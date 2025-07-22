using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class RolePermissionController
{
    private readonly IRolePermissionService _rolePermissionService;
    private readonly IAuthorizationService _authService;
    private readonly JwtHelper _jwt;
    public RolePermissionController(
        IRolePermissionService rolePermissionService,
        IAuthorizationService authService,
        JwtHelper jwt)
    {
        _rolePermissionService = rolePermissionService;
        _authService = authService;
        _jwt = jwt;
    }
    private async Task<(bool isAuthorized, int userId)> IsAuthorized(string token)
    {
        int? userId = _jwt.GetUserIdFromToken(token);
        if (userId == null) return (false, -1);
        var hasPermission = await _authService.HasAnyPermissionAsync(
            userId.Value,
            "ManageRoles", "ManagePermissions"
        );
        return (hasPermission, userId.Value);
    }
    /*
    POST /api/role-permission/assign
    */
    public async Task<string> AssignPermissionToRoleApi(int roleId, int permissionId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n kh�ng c� quy?n th?c hi?n h�nh d?ng n�y.";
        try
        {
            await _rolePermissionService.AddPermissionToRoleAsync(roleId, permissionId);
            return "G�n quy?n th�nh c�ng";
        }
        catch (Exception ex)
        {
            return "G�n quy?n th?t b?i: " + ex.Message;
        }
    }
    /*
    DELETE /api/role-permission/remove
    */
    public async Task<string> RemovePermissionFromRoleApi(int roleId, int permissionId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n kh�ng c� quy?n th?c hi?n h�nh d?ng n�y.";
        try
        {
            await _rolePermissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
            return "X�a quy?n th�nh c�ng";
        }
        catch (Exception ex)
        {
            return "X�a quy?n th?t b?i: " + ex.Message;
        }
    }
    /*
    POST /api/role-permission/assign-multiple
    */
    public async Task<string> AssignMultiplePermissionsToRoleApi(int roleId, List<int> permissionIds, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n kh�ng c� quy?n th?c hi?n h�nh d?ng n�y.";
        try
        {
            await _rolePermissionService.AddPermissionsToRoleAsync(roleId, permissionIds);
            return "G�n nhi?u quy?n th�nh c�ng";
        }
        catch (Exception ex)
        {
            return "G�n nhi?u quy?n th?t b?i: " + ex.Message;
        }
    }
    /*
    DELETE /api/role-permission/remove-multiple
    */
    public async Task<string> RemoveMultiplePermissionsFromRoleApi(int roleId, List<int> permissionIds, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n kh�ng c� quy?n th?c hi?n h�nh d?ng n�y.";
        try
        {
            await _rolePermissionService.RemovePermissionsFromRoleAsync(roleId, permissionIds);
            return "X�a nhi?u quy?n th�nh c�ng";
        }
        catch (Exception ex)
        {
            return "X�a nhi?u quy?n th?t b?i: " + ex.Message;
        }
    }
}
