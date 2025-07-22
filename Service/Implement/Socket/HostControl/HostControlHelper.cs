using ConsoleApp1.Model.DTO.Game;
namespace ConsoleApp1.Service.Implement.Socket.HostControl;
/// <summary>
/// Helper class cung cấp các utility methods cho host control
/// Chịu trách nhiệm tạo thông tin điều khiển và trạng thái cho host
/// </summary>
public static class HostControlHelper
{
    /// <summary>
    /// Lấy danh sách các điều khiển có sẵn cho host dựa trên trạng thái game
    /// </summary>
    /// <param name="gameRoom">Thông tin phòng game</param>
    /// <returns>Danh sách các điều khiển có thể sử dụng</returns>
    public static object GetAvailableHostControls(GameRoom gameRoom)
    {
        var controls = new List<object>();
        // Điều khiển cơ bản luôn có sẵn
        controls.Add(new { 
            action = "kick-player", 
            label = "Kick Player", 
            description = "Đuổi người chơi khỏi phòng",
            enabled = gameRoom.Players.Count > 1 
        });
        controls.Add(new { 
            action = "transfer-host", 
            label = "Transfer Host", 
            description = "Chuyển quyền host cho người khác",
            enabled = gameRoom.Players.Count > 1 
        });
        // Điều khiển game dựa trên trạng thái hiện tại
        switch (gameRoom.GameState.ToLower())
        {
            case "lobby":
            case "waiting":
                controls.Add(new { 
                    action = "start-game", 
                    label = "Bắt đầu Game", 
                    description = "Khởi động trò chơi",
                    enabled = gameRoom.Players.Count >= 1 
                });
                break;
            case "playing":
            case "question":
                controls.Add(new { 
                    action = "next-question", 
                    label = "Câu hỏi tiếp theo", 
                    description = "Chuyển sang câu hỏi tiếp theo",
                    enabled = true 
                });
                controls.Add(new { 
                    action = "pause-game", 
                    label = "Tạm dừng", 
                    description = "Tạm dừng trò chơi",
                    enabled = true 
                });
                controls.Add(new { 
                    action = "end-game", 
                    label = "Kết thúc Game", 
                    description = "Kết thúc trò chơi hiện tại",
                    enabled = true 
                });
                break;
            case "paused":
                controls.Add(new { 
                    action = "resume-game", 
                    label = "Tiếp tục", 
                    description = "Tiếp tục trò chơi",
                    enabled = true 
                });
                controls.Add(new { 
                    action = "end-game", 
                    label = "Kết thúc Game", 
                    description = "Kết thúc trò chơi hiện tại",
                    enabled = true 
                });
                break;
            case "finished":
                controls.Add(new { 
                    action = "restart-game", 
                    label = "Chơi lại", 
                    description = "Bắt đầu trò chơi mới",
                    enabled = true 
                });
                controls.Add(new { 
                    action = "new-game", 
                    label = "Game mới", 
                    description = "Tạo trò chơi mới với câu hỏi khác",
                    enabled = true 
                });
                break;
        }
        return new {
            availableControls = controls,
            totalControls = controls.Count,
            gameState = gameRoom.GameState
        };
    }
    /// <summary>
    /// Lấy trạng thái phòng chi tiết dành riêng cho host
    /// Bao gồm thông tin mà chỉ host mới cần biết
    /// </summary>
    /// <param name="gameRoom">Thông tin phòng game</param>
    /// <param name="hostSession">Session của host (optional)</param>
    /// <returns>Thông tin trạng thái phòng cho host</returns>
    public static object GetRoomStatusForHost(GameRoom gameRoom, HostControlSession? hostSession = null)
    {
        var playerStats = gameRoom.Players.Select(p => new {
            username = p.Username,
            isHost = p.IsHost,
            status = p.Status,
            joinTime = p.JoinTime,
            score = p.Score,
            isOnline = !string.IsNullOrEmpty(p.SocketId),
            lastActivity = GetPlayerLastActivity(p)
        }).ToList();
        var roomStatus = new {
            // Thông tin cơ bản
            roomCode = gameRoom.RoomCode,
            gameState = gameRoom.GameState,
            createdAt = gameRoom.CreatedAt,
            // Thông tin người chơi
            totalPlayers = gameRoom.Players.Count,
            onlinePlayers = gameRoom.Players.Count(p => !string.IsNullOrEmpty(p.SocketId)),
            players = playerStats,
            // Thông tin game
            currentQuestionIndex = gameRoom.CurrentQuestionIndex,
            totalQuestions = gameRoom.TotalQuestions,
            gameProgress = gameRoom.TotalQuestions > 0 ? 
                (double)gameRoom.CurrentQuestionIndex / gameRoom.TotalQuestions * 100 : 0,
            // Thông tin host (nếu có session)
            hostInfo = hostSession != null ? new {
                currentHost = hostSession.CurrentHostUsername,
                hostHistory = hostSession.HostHistory,
                recentActionsCount = hostSession.RecentActions.Count,
                lastActions = hostSession.RecentActions
                    .OrderByDescending(a => a.Timestamp)
                    .Take(5)
                    .Select(a => new {
                        action = a.Action,
                        timestamp = a.Timestamp,
                        data = a.Data
                    })
            } : null
        };
        return roomStatus;
    }
    /// <summary>
    /// Tạo thông báo chi tiết cho host
    /// </summary>
    /// <param name="message">Nội dung thông báo</param>
    /// <param name="gameRoom">Thông tin phòng game</param>
    /// <param name="hostSession">Session của host (optional)</param>
    /// <returns>Thông báo đầy đủ cho host</returns>
    public static object CreateHostNotification(string message, GameRoom gameRoom, HostControlSession? hostSession = null)
    {
        return new {
            message = message,
            timestamp = DateTime.UtcNow,
            roomCode = gameRoom.RoomCode,
            // Thông tin điều khiển
            hostControls = GetAvailableHostControls(gameRoom),
            // Trạng thái phòng
            roomStatus = GetRoomStatusForHost(gameRoom, hostSession),
            // Thống kê nhanh
            quickStats = new {
                playerCount = gameRoom.Players.Count,
                gameState = gameRoom.GameState,
                questionProgress = gameRoom.TotalQuestions > 0 ? 
                    $"{gameRoom.CurrentQuestionIndex + 1}/{gameRoom.TotalQuestions}" : "0/0"
            }
        };
    }
    /// <summary>
    /// Lấy thời gian hoạt động cuối của player
    /// </summary>
    /// <param name="player">Thông tin player</param>
    /// <returns>Thời gian hoạt động cuối</returns>
    private static DateTime GetPlayerLastActivity(GamePlayer player)
    {
        // Trong thực tế có thể lấy từ database hoặc cache
        // Hiện tại trả về join time
        return player.JoinTime ?? DateTime.UtcNow;
    }
    /// <summary>
    /// Kiểm tra xem hành động có hợp lệ trong trạng thái hiện tại không
    /// </summary>
    /// <param name="action">Tên hành động</param>
    /// <param name="gameState">Trạng thái game hiện tại</param>
    /// <returns>True nếu hành động hợp lệ</returns>
    public static bool IsActionValidForGameState(string action, string gameState)
    {
        return action.ToLower() switch
        {
            "start-game" => gameState.ToLower() is "lobby" or "waiting",
            "next-question" => gameState.ToLower() is "playing" or "question",
            "pause-game" => gameState.ToLower() is "playing" or "question",
            "resume-game" => gameState.ToLower() == "paused",
            "end-game" => gameState.ToLower() is "playing" or "question" or "paused",
            "restart-game" => gameState.ToLower() == "finished",
            "new-game" => gameState.ToLower() == "finished",
            "kick-player" => true, // Luôn cho phép kick player
            "transfer-host" => true, // Luôn cho phép chuyển host
            _ => false
        };
    }
}
