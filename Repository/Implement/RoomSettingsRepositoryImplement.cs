using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class RoomSettingsRepositoryImplement : IRoomSettingsRepository
{
    private readonly string ConnectionString;
    public RoomSettingsRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }
    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);
    public async Task<IEnumerable<RoomSetting>> GetSettingsByRoomIdAsync(int roomId)
    {
        const string query = @"
            SELECT room_id as RoomId, 
                   setting_key as SettingKey, 
                   setting_value as SettingValue 
            FROM room_settings 
            WHERE room_id = @RoomId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<RoomSetting>(query, new { RoomId = roomId });
    }
    public async Task<RoomSetting?> GetSettingAsync(int roomId, string settingKey)
    {
        const string query = @"
            SELECT room_id as RoomId, 
                   setting_key as SettingKey, 
                   setting_value as SettingValue 
            FROM room_settings 
            WHERE room_id = @RoomId AND setting_key = @SettingKey";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RoomSetting>(
            query, new { RoomId = roomId, SettingKey = settingKey });
    }
    public async Task AddSettingAsync(RoomSetting setting)
    {
        const string query = @"
            INSERT INTO room_settings (room_id, setting_key, setting_value)
            VALUES (@RoomId, @SettingKey, @SettingValue)";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, setting);
    }
    public async Task UpdateSettingAsync(RoomSetting setting)
    {
        const string query = @"
            UPDATE room_settings 
            SET setting_value = @SettingValue
            WHERE room_id = @RoomId AND setting_key = @SettingKey";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, setting);
    }
    public async Task DeleteSettingAsync(int roomId, string settingKey)
    {
        const string query = @"
            DELETE FROM room_settings 
            WHERE room_id = @RoomId AND setting_key = @SettingKey";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, new { RoomId = roomId, SettingKey = settingKey });
    }
    public async Task DeleteAllSettingsAsync(int roomId)
    {
        const string query = "DELETE FROM room_settings WHERE room_id = @RoomId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, new { RoomId = roomId });
    }
}
