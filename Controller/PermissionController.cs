using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class PermissionController
{
    private readonly IAuthorizationService _authService;
    private readonly JwtHelper _jwtHelper;
    private readonly IPermissionService _permissionService;
    public PermissionController(IAuthorizationService authService, JwtHelper jwtHelper,
        IPermissionService permissionService)
    {
        _authService = authService;
        _jwtHelper = jwtHelper;
        _permissionService = permissionService;
    }
     private async Task<(bool isAuthorized, int userId)> IsAuthorized(string token)
    {
        int? userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null) return (false, -1);
        var hasPermission = await _authService.HasPermissionAsync(userId.Value, "ManagePermissions");
        return (hasPermission, userId.Value);
    }
    public async Task<List<PermissionDTO>> GetAllPermissionsAsync(string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<PermissionDTO>();
        return await _permissionService.GetAllPermissionsAsync();
    }
    public async Task<PermissionDTO?> GetPermissionByIdAsync(int id, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;
        return await _permissionService.GetPermissionByIdAsync(id);
    }
    public async Task<string> CreatePermissionAsync(PermissionDTO permissionDto, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n không có quy?n th?c hi?n hành d?ng này.";
        var success = await _permissionService.AddPermissionAsync(permissionDto);
        return success ? "T?o permission thành công." : "Permission dã t?n t?i.";
    }
    public async Task<string> UpdatePermissionAsync(PermissionDTO permissionDto, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n không có quy?n th?c hi?n hành d?ng này.";
        var success = await _permissionService.UpdatePermissionAsync(permissionDto);
        return success ? "C?p nh?t permission thành công." : "Permission không t?n t?i.";
    }
    public async Task<string> DeletePermissionAsync(int id, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "B?n không có quy?n th?c hi?n hành d?ng này.";
        var success = await _permissionService.DeletePermissionAsync(id);
        return success ? "Xóa permission thành công." : "Không tìm th?y permission.";
    }
    public async Task<bool> PermissionNameExistsAsync(string permissionName, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return false;
        return await _permissionService.PermissionNameExistsAsync(permissionName);
    }
}
