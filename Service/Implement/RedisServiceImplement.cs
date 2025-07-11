using ConsoleApp1.Config;
using ConsoleApp1.Service.Interface;
using StackExchange.Redis;

namespace ConsoleApp1.Service.Implement;

public class RedisServiceImplement : IRedisService
{
    private readonly IDatabase db;
    public RedisServiceImplement(RedisConnection connection)
    {
        db = connection.GetDatabase();
    }

    public async Task SetPermissionsAsync(int userId, IEnumerable<string> permissions)
    {
        string key = $"permissions:{userId}";
        await db.KeyDeleteAsync(key);

        if (permissions != null && permissions.Any())
        {
            await db.SetAddAsync(key, permissions.Select(p => (RedisValue)p).ToArray());
            Console.WriteLine($"[REDIS] Đã lưu {permissions.Count()} quyền cho user {userId}");
        }
        else
        {
            Console.WriteLine($"[WARNING] Không có quyền nào để set vào Redis cho user {userId} không tạo được phòng, hãy kiểm tra lại");
        }
    }


    public async Task AddPermissionAsync(int userId, string permission)
    {
        await db.SetAddAsync($"permissions:{userId}", permission);
    }

    public async Task RemovePermissionAsync(int userId, string permission)
    {
        await db.SetRemoveAsync($"permissions:{userId}", permission);
    }

    public async Task<IEnumerable<string>> GetPermissionsAsync(int userId)
    {
        var values = await db.SetMembersAsync($"permissions:{userId}");
        return values.Select(v => v.ToString());
    }

    public async Task<bool> HasPermissionAsync(int userId, string permission)
    {
        return await db.SetContainsAsync($"permissions:{userId}", permission);
    }

    public async Task SetRefreshTokenAsync(int userId, string refreshToken, TimeSpan expiry)
    {
        await db.StringSetAsync($"refresh:{userId}", refreshToken, expiry);
    }

    public async Task<string?> GetRefreshTokenAsync(int userId)
    {
        var token = await db.StringGetAsync($"refresh:{userId}");
        return token.HasValue ? token.ToString() : null;
    }

    public async Task DeleteRefreshTokenAsync(int userId)
    {
        await db.KeyDeleteAsync($"refresh:{userId}");
    }

    public async Task SetStringAsync(string key, string value, TimeSpan expiry)
    {
        await db.StringSetAsync(key, value, expiry);
    }

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task DeleteAsync(string key)
    {
        await db.KeyDeleteAsync(key);
    }
}
