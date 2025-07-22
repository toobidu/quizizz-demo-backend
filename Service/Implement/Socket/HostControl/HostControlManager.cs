using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Quản lý các session điều khiển host và thông tin phòng
/// Chịu trách nhiệm lưu trữ và quản lý trạng thái host control
/// </summary>
public class HostControlManager
{
    // Dictionary lưu trữ host control sessions theo room code
    private readonly ConcurrentDictionary<string, HostControlSession> _hostSessions = new();
    // Dictionary lưu trữ các phòng game (shared reference)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    public HostControlManager(ConcurrentDictionary<string, GameRoom> gameRooms)
    {
        _gameRooms = gameRooms;
    }
    /// <summary>
    /// Lấy hoặc tạo mới host session cho phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="hostUsername">Username của host hiện tại</param>
    /// <returns>Host control session</returns>
    public HostControlSession GetOrCreateHostSession(string roomCode, string hostUsername)
    {
        if (!_hostSessions.TryGetValue(roomCode, out var hostSession))
        {
            hostSession = new HostControlSession 
            { 
                RoomCode = roomCode,
                CurrentHostUsername = hostUsername
            };
            _hostSessions[roomCode] = hostSession;
        }
        return hostSession;
    }
    /// <summary>
    /// Cập nhật host hiện tại của phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="newHostUsername">Username host mới</param>
    /// <param name="oldHostUsername">Username host cũ (optional)</param>
    public void UpdateCurrentHost(string roomCode, string newHostUsername, string? oldHostUsername = null)
    {
        var hostSession = GetOrCreateHostSession(roomCode, newHostUsername);
        // Thêm host cũ vào lịch sử nếu có
        if (!string.IsNullOrEmpty(oldHostUsername) && oldHostUsername != newHostUsername)
        {
            if (!hostSession.HostHistory.Contains(oldHostUsername))
            {
                hostSession.HostHistory.Add(oldHostUsername);
            }
        }
        hostSession.CurrentHostUsername = newHostUsername;
        hostSession.UpdateHostActivity(newHostUsername);
    }
    /// <summary>
    /// Thêm hành động của host vào lịch sử
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="action">Hành động cần thêm</param>
    public void AddHostAction(string roomCode, HostAction action)
    {
        if (_hostSessions.TryGetValue(roomCode, out var hostSession))
        {
            hostSession.AddAction(action);
        }
    }
    /// <summary>
    /// Lấy host session của phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <returns>Host session hoặc null nếu không tồn tại</returns>
    public HostControlSession? GetHostSession(string roomCode)
    {
        _hostSessions.TryGetValue(roomCode, out var hostSession);
        return hostSession;
    }
    /// <summary>
    /// Xóa host session khi phòng bị đóng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public void RemoveHostSession(string roomCode)
    {
        if (_hostSessions.TryRemove(roomCode, out var removedSession))
        {
        }
    }
    /// <summary>
    /// Kiểm tra xem user có phải là host của phòng không
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Username cần kiểm tra</param>
    /// <returns>True nếu là host</returns>
    public bool IsUserHost(string roomCode, string username)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return false;
        }
        var host = gameRoom.Players.FirstOrDefault(p => p.IsHost);
        return host?.Username == username;
    }
    /// <summary>
    /// Lấy thông tin host hiện tại của phòng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <returns>GamePlayer host hoặc null nếu không tìm thấy</returns>
    public GamePlayer? GetCurrentHost(string roomCode)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            return null;
        }
        return gameRoom.Players.FirstOrDefault(p => p.IsHost);
    }
}
