namespace ConsoleApp1.Model.Entity.Rooms;

public class RoomSettings
{
    public int RoomId { get; set; }
    public string SettingKey { get; set; }
    public string SettingValue { get; set; }

    public RoomSettings() { }

    public RoomSettings(int roomId, string settingKey, string settingValue)
    {
        RoomId = roomId;
        SettingKey = settingKey;
        SettingValue = settingValue;
    }
}