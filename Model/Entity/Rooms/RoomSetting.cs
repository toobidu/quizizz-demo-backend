namespace ConsoleApp1.Model.Entity.Rooms;

public class RoomSetting
{
    public int RoomId { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;

    public RoomSetting() { }

    public RoomSetting(int roomId, string settingKey, string settingValue)
    {
        RoomId = roomId;
        SettingKey = settingKey;
        SettingValue = settingValue;
    }
}