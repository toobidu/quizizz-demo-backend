namespace ConsoleApp1.Service.Interface;
public interface IAuthorizationService
{
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task<bool> HasAnyPermissionAsync(int userId, params string[] requiredPermissions);
}
