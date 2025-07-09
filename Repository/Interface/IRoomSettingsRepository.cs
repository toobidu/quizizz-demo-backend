using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface IRoomSettingsRepository
{
    Task<IEnumerable<RoomSetting>> GetSettingsByRoomIdAsync(int roomId);
    Task<RoomSetting?> GetSettingAsync(int roomId, string settingKey);
    Task AddSettingAsync(RoomSetting setting);
    Task UpdateSettingAsync(RoomSetting setting);
    Task DeleteSettingAsync(int roomId, string settingKey);
    Task DeleteAllSettingsAsync(int roomId);
}