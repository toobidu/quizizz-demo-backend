using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
public class AuthorizationServiceImplement : IAuthorizationService
{
    private readonly IRedisService _redisService;
    private readonly IPermissionRepository _permissionRepo;
    public AuthorizationServiceImplement(IRedisService redisService, IPermissionRepository permissionRepo)
    {
        _redisService = redisService;
        _permissionRepo = permissionRepo;
    }
    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        var permissions = await _redisService.GetPermissionsAsync(userId);
        if (permissions == null || !permissions.Any())
        {
            permissions = (await _permissionRepo.GetPermissionsByUserIdAsync(userId)).ToList();
            await _redisService.SetPermissionsAsync(userId, permissions);
        }
        return permissions.Contains(permission);
    }
    public async Task<bool> HasAnyPermissionAsync(int userId, params string[] requiredPermissions)
    {
        var userPermissions = (await _redisService.GetPermissionsAsync(userId)).ToList();
        if (!userPermissions.Any())
        {
            userPermissions = (await _permissionRepo.GetPermissionsByUserIdAsync(userId)).ToList();
            await _redisService.SetPermissionsAsync(userId, userPermissions);
        }
        return requiredPermissions.Any(p => userPermissions.Contains(p));
    }
}
