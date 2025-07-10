namespace ConsoleApp1.Service.Interface;

public interface IRedisService
{
    Task SetPermissionsAsync(int userId, IEnumerable<string> permissions);
    Task AddPermissionAsync(int userId, string permission);
    Task RemovePermissionAsync(int userId, string permission);
    Task<IEnumerable<string>> GetPermissionsAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task SetRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiry);
    Task<string?> GetRefreshTokenAsync(int userId);
    Task DeleteRefreshTokenAsync(int userId);
    Task SetStringAsync(string key, string value, TimeSpan expiry);
    Task<string?> GetStringAsync(string key);
    Task DeleteAsync(string key);
}
