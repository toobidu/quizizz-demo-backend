namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Class lưu trữ các hành động của host trong phòng game
/// </summary>
public class HostAction
{
    /// <summary>
    /// Tên hành động (kick-player, next-question, etc.)
    /// </summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Username của host thực hiện hành động
    /// </summary>
    public string HostUsername { get; set; } = string.Empty;
    /// <summary>
    /// Thời gian thực hiện hành động
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Dữ liệu bổ sung của hành động (tùy thuộc vào loại action)
    /// </summary>
    public object? Data { get; set; }
}
