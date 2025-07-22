namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Class quản lý host control session của một phòng game
/// Lưu trữ thông tin về host hiện tại, lịch sử host và các hoạt động
/// </summary>
public class HostControlSession
{
    /// <summary>
    /// Mã phòng game
    /// </summary>
    public string RoomCode { get; set; } = string.Empty;
    /// <summary>
    /// Username của host hiện tại
    /// </summary>
    public string CurrentHostUsername { get; set; } = string.Empty;
    /// <summary>
    /// Lịch sử các host trước đó trong phòng
    /// </summary>
    public List<string> HostHistory { get; set; } = new();
    /// <summary>
    /// Thời gian hoạt động cuối cùng của từng host
    /// Key: username, Value: thời gian hoạt động cuối
    /// </summary>
    public Dictionary<string, DateTime> LastHostActivity { get; set; } = new();
    /// <summary>
    /// Có cho phép host điều khiển game hay không
    /// </summary>
    public bool IsGameControlEnabled { get; set; } = true;
    /// <summary>
    /// Danh sách các hành động gần đây của host
    /// Giới hạn 50 actions để tránh memory leak
    /// </summary>
    public List<HostAction> RecentActions { get; set; } = new();
    /// <summary>
    /// Thêm một hành động mới vào lịch sử
    /// Tự động giới hạn số lượng actions để tránh memory leak
    /// </summary>
    /// <param name="action">Hành động cần thêm</param>
    public void AddAction(HostAction action)
    {
        RecentActions.Add(action);
        // Giữ chỉ 50 actions gần nhất để tránh memory leak
        if (RecentActions.Count > 50)
        {
            RecentActions.RemoveAt(0);
        }
    }
    /// <summary>
    /// Cập nhật thời gian hoạt động cuối của host
    /// </summary>
    /// <param name="hostUsername">Username của host</param>
    public void UpdateHostActivity(string hostUsername)
    {
        LastHostActivity[hostUsername] = DateTime.UtcNow;
    }
}
