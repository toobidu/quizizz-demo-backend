using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.HostControl;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
namespace ConsoleApp1.Service.Implement.Socket;
/// <summary>
/// Service chính xử lý điều khiển host qua WebSocket
/// Đã được tách nhỏ thành các component riêng biệt để dễ bảo trì
/// Chịu trách nhiệm:
/// 1. Gửi thông báo riêng cho host
/// 2. Xử lý các lệnh điều khiển từ host
/// 3. Quản lý quyền host và phân quyền
/// 4. Điều phối các component con
/// </summary>
public class HostControlSocketServiceImplement : IHostControlSocketService
{
    // Shared dictionaries với các service khác
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    // Các component được tách riêng
    private readonly HostControlManager _hostControlManager;
    private readonly HostActionHandler _hostActionHandler;
    private readonly ISocketMessageSender _messageSender;
    public HostControlSocketServiceImplement(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
        // Khởi tạo các component
        _messageSender = new SocketMessageSender(_gameRooms, _connections);
        _hostControlManager = new HostControlManager(_gameRooms);
        _hostActionHandler = new HostActionHandler(_gameRooms, _connections, _hostControlManager, _messageSender);
    }
    /// <summary>
    /// Gửi thông báo chỉ cho host của phòng
    /// Sử dụng các component đã tách để xử lý logic
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="message">Thông báo cần gửi cho host</param>
    public async Task NotifyHostOnlyAsync(string roomCode, string message)
    {
        try
        {
            // Tìm host của phòng
            var host = _hostControlManager.GetCurrentHost(roomCode);
            if (host == null)
            {
                return;
            }
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return;
            }
            // Lấy hoặc tạo host session
            var hostSession = _hostControlManager.GetOrCreateHostSession(roomCode, host.Username);
            hostSession.UpdateHostActivity(host.Username);
            // Tạo thông báo chi tiết cho host sử dụng helper
            var hostNotification = HostControlHelper.CreateHostNotification(message, gameRoom, hostSession);
            // Gửi message riêng cho host
            await _messageSender.SendToPlayerAsync(roomCode, host.Username, "host-notification", hostNotification);
            // Log hành động
            var hostAction = new HostAction
            {
                Action = "notification-sent",
                HostUsername = host.Username,
                Data = new { message = message }
            };
            _hostControlManager.AddHostAction(roomCode, hostAction);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Host yêu cầu câu hỏi tiếp theo
    /// Delegate cho HostActionHandler để xử lý
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task RequestNextQuestionAsync(string roomCode)
    {
        // Tìm host hiện tại
        var host = _hostControlManager.GetCurrentHost(roomCode);
        if (host == null)
        {
            return;
        }
        // Delegate cho HostActionHandler
        await _hostActionHandler.HandleNextQuestionRequestAsync(roomCode, host.Username);
    }
    /// <summary>
    /// Chuyển quyền host cho người khác
    /// Delegate cho HostActionHandler để xử lý
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="currentHostUsername">Username host hiện tại</param>
    /// <param name="newHostUsername">Username host mới</param>
    public async Task TransferHostAsync(string roomCode, string currentHostUsername, string newHostUsername)
    {
        // Delegate cho HostActionHandler
        await _hostActionHandler.TransferHostAsync(roomCode, currentHostUsername, newHostUsername);
    }
    /// <summary>
    /// Kick player khỏi phòng (chỉ host mới có quyền)
    /// Delegate cho HostActionHandler để xử lý
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="hostUsername">Username của host</param>
    /// <param name="playerToKick">Username của player cần kick</param>
    public async Task KickPlayerAsync(string roomCode, string hostUsername, string playerToKick)
    {
        // Delegate cho HostActionHandler
        await _hostActionHandler.KickPlayerAsync(roomCode, hostUsername, playerToKick);
    }
    /// <summary>
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <returns>Thông tin trạng thái host control</returns>
    public object GetHostControlStatus(string roomCode)
    {
        var hostSession = _hostControlManager.GetHostSession(roomCode);
        var currentHost = _hostControlManager.GetCurrentHost(roomCode);
        return new {
            roomCode = roomCode,
            hasHostSession = hostSession != null,
            currentHost = currentHost?.Username,
            hostSessionInfo = hostSession != null ? new {
                hostHistory = hostSession.HostHistory,
                recentActionsCount = hostSession.RecentActions.Count,
                isGameControlEnabled = hostSession.IsGameControlEnabled
            } : null
        };
    }
    /// <summary>
    /// Cleanup khi phòng bị đóng
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public void CleanupRoom(string roomCode)
    {
        _hostControlManager.RemoveHostSession(roomCode);
    }
}
