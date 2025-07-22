using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Xử lý các hành động cụ thể của host
/// Chịu trách nhiệm thực thi các lệnh điều khiển từ host
/// </summary>
public class HostActionHandler
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly HostControlManager _hostControlManager;
    private readonly ISocketMessageSender _messageSender;
    public HostActionHandler(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections,
        HostControlManager hostControlManager,
        ISocketMessageSender messageSender)
    {
        _gameRooms = gameRooms;
        _connections = connections;
        _hostControlManager = hostControlManager;
        _messageSender = messageSender;
    }
    /// <summary>
    /// Chuyển quyền host cho người khác
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="currentHostUsername">Username host hiện tại</param>
    /// <param name="newHostUsername">Username host mới</param>
    public async Task TransferHostAsync(string roomCode, string currentHostUsername, string newHostUsername)
    {
        try
        {
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return;
            }
            var currentHost = gameRoom.Players.FirstOrDefault(p => p.Username == currentHostUsername && p.IsHost);
            var newHost = gameRoom.Players.FirstOrDefault(p => p.Username == newHostUsername);
            if (currentHost == null || newHost == null)
            {
                await _messageSender.SendToPlayerAsync(roomCode, currentHostUsername, "host-error", new {
                    message = "Không thể chuyển quyền host: người dùng không hợp lệ"
                });
                return;
            }
            // Thực hiện chuyển quyền host
            currentHost.IsHost = false;
            newHost.IsHost = true;
            // Cập nhật host manager
            _hostControlManager.UpdateCurrentHost(roomCode, newHostUsername, currentHostUsername);
            // Log hành động
            var hostAction = new HostAction
            {
                Action = "host-transferred",
                HostUsername = currentHostUsername,
                Data = new { 
                    oldHost = currentHostUsername,
                    newHost = newHostUsername
                }
            };
            _hostControlManager.AddHostAction(roomCode, hostAction);
            // Thông báo cho tất cả player
            await _messageSender.BroadcastToRoomAsync(roomCode, "host-changed", new {
                oldHost = currentHostUsername,
                newHost = newHostUsername,
                message = $"{newHostUsername} đã trở thành host mới của phòng"
            });
            // Gửi thông báo riêng cho host mới
            var hostNotification = HostControlHelper.CreateHostNotification(
                "Bạn đã trở thành host của phòng", 
                gameRoom, 
                _hostControlManager.GetHostSession(roomCode)
            );
            await _messageSender.SendToPlayerAsync(roomCode, newHostUsername, "you-are-host", hostNotification);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Kick player khỏi phòng (chỉ host mới có quyền)
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="hostUsername">Username của host</param>
    /// <param name="playerToKick">Username của player cần kick</param>
    public async Task KickPlayerAsync(string roomCode, string hostUsername, string playerToKick)
    {
        try
        {
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return;
            }
            // Kiểm tra quyền host
            if (!_hostControlManager.IsUserHost(roomCode, hostUsername))
            {
                await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Bạn không có quyền kick người chơi"
                });
                return;
            }
            var playerToRemove = gameRoom.Players.FirstOrDefault(p => p.Username == playerToKick);
            if (playerToRemove == null)
            {
                await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Không tìm thấy người chơi cần kick"
                });
                return;
            }
            // Không cho phép kick chính mình
            if (playerToKick == hostUsername)
            {
                await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Không thể kick chính mình"
                });
                return;
            }
            // Thông báo cho player bị kick
            await _messageSender.SendToPlayerAsync(roomCode, playerToKick, "kicked-from-room", new {
                message = "Bạn đã bị kick khỏi phòng bởi host",
                hostUsername = hostUsername,
                reason = "Bị host kick khỏi phòng"
            });
            // Xóa player khỏi phòng
            gameRoom.Players.Remove(playerToRemove);
            // Log hành động
            var hostAction = new HostAction
            {
                Action = "player-kicked",
                HostUsername = hostUsername,
                Data = new { kickedPlayer = playerToKick }
            };
            _hostControlManager.AddHostAction(roomCode, hostAction);
            // Thông báo cho tất cả player còn lại
            await _messageSender.BroadcastToRoomAsync(roomCode, "player-kicked", new {
                kickedPlayer = playerToKick,
                hostUsername = hostUsername,
                message = $"{playerToKick} đã bị kick khỏi phòng",
                remainingPlayers = gameRoom.Players.Count
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý yêu cầu câu hỏi tiếp theo từ host
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="hostUsername">Username của host</param>
    public async Task HandleNextQuestionRequestAsync(string roomCode, string hostUsername)
    {
        try
        {
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return;
            }
            // Kiểm tra quyền host
            if (!_hostControlManager.IsUserHost(roomCode, hostUsername))
            {
                await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Bạn không có quyền điều khiển game"
                });
                return;
            }
            // Kiểm tra trạng thái game
            if (!HostControlHelper.IsActionValidForGameState("next-question", gameRoom.GameState))
            {
                await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Không thể chuyển câu hỏi trong trạng thái hiện tại",
                    currentState = gameRoom.GameState
                });
                return;
            }
            var nextQuestionIndex = gameRoom.CurrentQuestionIndex + 1;
            // Log hành động
            var hostAction = new HostAction
            {
                Action = "next-question-requested",
                HostUsername = hostUsername,
                Data = new { 
                    currentQuestionIndex = gameRoom.CurrentQuestionIndex,
                    requestedQuestionIndex = nextQuestionIndex
                }
            };
            _hostControlManager.AddHostAction(roomCode, hostAction);
            // Thông báo cho host về việc xử lý request
            await _messageSender.SendToPlayerAsync(roomCode, hostUsername, "host-action-processed", new {
                action = "next-question-requested",
                status = "processing",
                message = "Đang xử lý yêu cầu chuyển câu hỏi tiếp theo..."
            });
            // Broadcast thông báo cho tất cả player
            await _messageSender.BroadcastToRoomAsync(roomCode, "host-action", new {
                action = "next-question-requested",
                hostUsername = hostUsername,
                message = $"{hostUsername} đã yêu cầu chuyển câu hỏi tiếp theo"
            });
            // Simulate việc gửi câu hỏi tiếp theo (trong thực tế sẽ gọi GameFlowService)
            await SimulateNextQuestionAsync(roomCode, nextQuestionIndex);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Simulate việc gửi câu hỏi tiếp theo
    /// Trong thực tế sẽ gọi GameFlowService
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="questionIndex">Index câu hỏi</param>
    private async Task SimulateNextQuestionAsync(string roomCode, int questionIndex)
    {
        // Trong thực tế sẽ gọi:
        // await _gameFlowService.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
        await _messageSender.BroadcastToRoomAsync(roomCode, "next-question-ready", new {
            questionIndex = questionIndex,
            message = $"Câu hỏi số {questionIndex + 1} đã sẵn sàng",
            timestamp = DateTime.UtcNow
        });
    }
}
/// <summary>
/// Interface cho việc gửi message qua WebSocket
/// Để tách biệt logic gửi message khỏi business logic
/// </summary>
public interface ISocketMessageSender
{
    Task BroadcastToRoomAsync(string roomCode, string eventName, object data);
    Task SendToPlayerAsync(string roomCode, string username, string eventName, object data);
}
