using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service xử lý điều khiển host qua WebSocket - Chịu trách nhiệm:
/// 1. Gửi thông báo riêng cho host
/// 2. Xử lý các lệnh điều khiển từ host
/// 3. Quản lý quyền host (start game, kick player, etc.)
/// 4. Chuyển quyền host khi cần thiết
/// </summary>
public class HostControlSocketServiceImplement : IHostControlSocketService
{
    // Dictionary lưu trữ các phòng game (shared với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (shared với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    
    // Dictionary lưu trữ host control sessions
    private readonly ConcurrentDictionary<string, HostControlSession> _hostSessions = new();

    /// <summary>
    /// Class quản lý host control session của một phòng
    /// </summary>
    private class HostControlSession
    {
        public string RoomCode { get; set; } = string.Empty;
        public string CurrentHostUsername { get; set; } = string.Empty;
        public List<string> HostHistory { get; set; } = new(); // Lịch sử các host trước đó
        public Dictionary<string, DateTime> LastHostActivity { get; set; } = new();
        public bool IsGameControlEnabled { get; set; } = true;
        public List<HostAction> RecentActions { get; set; } = new();
    }

    /// <summary>
    /// Class lưu trữ các hành động của host
    /// </summary>
    private class HostAction
    {
        public string Action { get; set; } = string.Empty;
        public string HostUsername { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object? Data { get; set; }
    }

    /// <summary>
    /// Gửi thông báo chỉ cho host của phòng
    /// Dùng để gửi các thông tin điều khiển mà chỉ host cần biết
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="message">Thông báo cần gửi cho host</param>
    public async Task NotifyHostOnlyAsync(string roomCode, string message)
    {
        Console.WriteLine($"[HOST] Sending host-only notification to room {roomCode}: {message}");
        
        try
        {
            // Tìm host của phòng
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                Console.WriteLine($"[HOST] Room {roomCode} not found");
                return;
            }

            var host = gameRoom.Players.FirstOrDefault(p => p.IsHost);
            if (host == null)
            {
                Console.WriteLine($"[HOST] No host found in room {roomCode}");
                return;
            }

            // Cập nhật host session
            if (!_hostSessions.TryGetValue(roomCode, out var hostSession))
            {
                hostSession = new HostControlSession 
                { 
                    RoomCode = roomCode,
                    CurrentHostUsername = host.Username
                };
                _hostSessions[roomCode] = hostSession;
            }

            // Cập nhật thời gian hoạt động cuối của host
            hostSession.LastHostActivity[host.Username] = DateTime.UtcNow;

            // Tạo thông báo chi tiết cho host
            var hostNotification = new {
                message = message,
                timestamp = DateTime.UtcNow,
                roomCode = roomCode,
                hostControls = GetAvailableHostControls(gameRoom),
                roomStatus = GetRoomStatusForHost(gameRoom),
                playerCount = gameRoom.Players.Count,
                gameState = gameRoom.GameState
            };

            // Gửi message riêng cho host
            await SendToPlayerAsync(roomCode, host.Username, "host-notification", hostNotification);

            // Log host action
            hostSession.RecentActions.Add(new HostAction
            {
                Action = "notification-sent",
                HostUsername = host.Username,
                Data = new { message = message }
            });

            // Giữ chỉ 50 actions gần nhất
            if (hostSession.RecentActions.Count > 50)
            {
                hostSession.RecentActions.RemoveAt(0);
            }

            Console.WriteLine($"[HOST] Host notification sent to {host.Username} in room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HOST] Error sending host notification to room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Host yêu cầu câu hỏi tiếp theo
    /// Dùng trong chế độ host-controlled (host điều khiển tốc độ game)
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task RequestNextQuestionAsync(string roomCode)
    {
        Console.WriteLine($"[HOST] Host requested next question for room {roomCode}");
        
        try
        {
            // Kiểm tra phòng tồn tại
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                Console.WriteLine($"[HOST] Room {roomCode} not found");
                return;
            }

            // Tìm host
            var host = gameRoom.Players.FirstOrDefault(p => p.IsHost);
            if (host == null)
            {
                Console.WriteLine($"[HOST] No host found in room {roomCode}");
                return;
            }

            // Kiểm tra trạng thái game có cho phép chuyển câu không
            if (gameRoom.GameState != "playing" && gameRoom.GameState != "question")
            {
                await SendToPlayerAsync(roomCode, host.Username, "host-error", new {
                    message = "Không thể chuyển câu hỏi trong trạng thái hiện tại",
                    currentState = gameRoom.GameState
                });
                return;
            }

            // Cập nhật host session
            if (!_hostSessions.TryGetValue(roomCode, out var hostSession))
            {
                hostSession = new HostControlSession 
                { 
                    RoomCode = roomCode,
                    CurrentHostUsername = host.Username
                };
                _hostSessions[roomCode] = hostSession;
            }

            // Kiểm tra xem có câu hỏi tiếp theo không
            var nextQuestionIndex = gameRoom.CurrentQuestionIndex + 1;
            
            // Log host action
            hostSession.RecentActions.Add(new HostAction
            {
                Action = "next-question-requested",
                HostUsername = host.Username,
                Data = new { 
                    currentQuestionIndex = gameRoom.CurrentQuestionIndex,
                    requestedQuestionIndex = nextQuestionIndex
                }
            });

            // Thông báo cho host về việc xử lý request
            await SendToPlayerAsync(roomCode, host.Username, "host-action-processed", new {
                action = "next-question-requested",
                status = "processing",
                message = "Đang xử lý yêu cầu chuyển câu hỏi tiếp theo..."
            });

            // Broadcast thông báo cho tất cả player (không bao gồm câu hỏi cụ thể)
            await BroadcastToRoomAsync(roomCode, "host-action", new {
                action = "next-question-requested",
                hostUsername = host.Username,
                message = $"{host.Username} đã yêu cầu chuyển câu hỏi tiếp theo"
            });

            // Thực tế sẽ cần gọi GameFlowService để gửi câu hỏi tiếp theo
            // Ở đây chỉ simulate
            await SimulateNextQuestionAsync(roomCode, nextQuestionIndex);

            Console.WriteLine($"[HOST] Next question request processed for room {roomCode} by {host.Username}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HOST] Error processing next question request for room {roomCode}: {ex.Message}");
        }
    }

    #region Additional Host Control Methods

    /// <summary>
    /// Chuyển quyền host cho người khác
    /// </summary>
    public async Task TransferHostAsync(string roomCode, string currentHostUsername, string newHostUsername)
    {
        Console.WriteLine($"[HOST] Transferring host from {currentHostUsername} to {newHostUsername} in room {roomCode}");
        
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
                Console.WriteLine($"[HOST] Invalid host transfer: current host or new host not found");
                return;
            }

            // Chuyển quyền host
            currentHost.IsHost = false;
            newHost.IsHost = true;

            // Cập nhật host session
            if (_hostSessions.TryGetValue(roomCode, out var hostSession))
            {
                hostSession.HostHistory.Add(currentHostUsername);
                hostSession.CurrentHostUsername = newHostUsername;
                hostSession.RecentActions.Add(new HostAction
                {
                    Action = "host-transferred",
                    HostUsername = currentHostUsername,
                    Data = new { 
                        oldHost = currentHostUsername,
                        newHost = newHostUsername
                    }
                });
            }

            // Thông báo cho tất cả player
            await BroadcastToRoomAsync(roomCode, "host-changed", new {
                oldHost = currentHostUsername,
                newHost = newHostUsername,
                message = $"{newHostUsername} đã trở thành host mới"
            });

            // Gửi thông báo riêng cho host mới
            await SendToPlayerAsync(roomCode, newHostUsername, "you-are-host", new {
                message = "Bạn đã trở thành host của phòng",
                hostControls = GetAvailableHostControls(gameRoom),
                roomStatus = GetRoomStatusForHost(gameRoom)
            });

            Console.WriteLine($"[HOST] Host transferred successfully in room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HOST] Error transferring host in room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Kick player khỏi phòng (chỉ host mới có quyền)
    /// </summary>
    public async Task KickPlayerAsync(string roomCode, string hostUsername, string playerToKick)
    {
        Console.WriteLine($"[HOST] {hostUsername} attempting to kick {playerToKick} from room {roomCode}");
        
        try
        {
            if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                return;
            }

            var host = gameRoom.Players.FirstOrDefault(p => p.Username == hostUsername && p.IsHost);
            if (host == null)
            {
                await SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Bạn không có quyền kick player"
                });
                return;
            }

            var playerToRemove = gameRoom.Players.FirstOrDefault(p => p.Username == playerToKick);
            if (playerToRemove == null)
            {
                await SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Không tìm thấy player cần kick"
                });
                return;
            }

            // Không cho phép kick chính mình
            if (playerToKick == hostUsername)
            {
                await SendToPlayerAsync(roomCode, hostUsername, "host-error", new {
                    message = "Không thể kick chính mình"
                });
                return;
            }

            // Thông báo cho player bị kick
            await SendToPlayerAsync(roomCode, playerToKick, "kicked-from-room", new {
                message = "Bạn đã bị kick khỏi phòng bởi host",
                hostUsername = hostUsername
            });

            // Remove player khỏi phòng
            gameRoom.Players.Remove(playerToRemove);

            // Log action
            if (_hostSessions.TryGetValue(roomCode, out var hostSession))
            {
                hostSession.RecentActions.Add(new HostAction
                {
                    Action = "player-kicked",
                    HostUsername = hostUsername,
                    Data = new { kickedPlayer = playerToKick }
                });
            }

            // Thông báo cho tất cả player còn lại
            await BroadcastToRoomAsync(roomCode, "player-kicked", new {
                kickedPlayer = playerToKick,
                hostUsername = hostUsername,
                message = $"{playerToKick} đã bị kick khỏi phòng",
                remainingPlayers = gameRoom.Players.Count
            });

            Console.WriteLine($"[HOST] Player {playerToKick} kicked from room {roomCode} by {hostUsername}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HOST] Error kicking player from room {roomCode}: {ex.Message}");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Lấy danh sách các điều khiển có sẵn cho host
    /// </summary>
    private object GetAvailableHostControls(GameRoom gameRoom)
    {
        var controls = new List<object>();

        // Điều khiển cơ bản luôn có
        controls.Add(new { 
            action = "kick-player", 
            label = "Kick Player", 
            enabled = gameRoom.Players.Count > 1 
        });
        
        controls.Add(new { 
            action = "transfer-host", 
            label = "Transfer Host", 
            enabled = gameRoom.Players.Count > 1 
        });

        // Điều khiển game dựa trên trạng thái
        switch (gameRoom.GameState.ToLower())
        {
            case "lobby":
            case "waiting":
                controls.Add(new { 
                    action = "start-game", 
                    label = "Start Game", 
                    enabled = gameRoom.Players.Count >= 1 
                });
                break;
                
            case "playing":
            case "question":
                controls.Add(new { 
                    action = "next-question", 
                    label = "Next Question", 
                    enabled = true 
                });
                controls.Add(new { 
                    action = "end-game", 
                    label = "End Game", 
                    enabled = true 
                });
                break;
                
            case "finished":
                controls.Add(new { 
                    action = "restart-game", 
                    label = "Restart Game", 
                    enabled = true 
                });
                break;
        }

        return controls;
    }

    /// <summary>
    /// Lấy trạng thái phòng chi tiết cho host
    /// </summary>
    private object GetRoomStatusForHost(GameRoom gameRoom)
    {
        return new {
            roomCode = gameRoom.RoomCode,
            gameState = gameRoom.GameState,
            totalPlayers = gameRoom.Players.Count,
            currentQuestionIndex = gameRoom.CurrentQuestionIndex,
            totalQuestions = gameRoom.TotalQuestions,
            players = gameRoom.Players.Select(p => new {
                username = p.Username,
                isHost = p.IsHost,
                status = p.Status,
                joinTime = p.JoinTime,
                score = p.Score
            }).ToList()
        };
    }

    /// <summary>
    /// Simulate việc gửi câu hỏi tiếp theo (thực tế sẽ gọi GameFlowService)
    /// </summary>
    private async Task SimulateNextQuestionAsync(string roomCode, int questionIndex)
    {
        // Trong thực tế sẽ gọi:
        // await _gameFlowService.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
        
        // Hiện tại chỉ simulate
        await BroadcastToRoomAsync(roomCode, "next-question-ready", new {
            questionIndex = questionIndex,
            message = $"Câu hỏi số {questionIndex + 1} sẵn sàng",
            timestamp = DateTime.UtcNow
        });
        
        Console.WriteLine($"[HOST] Simulated next question {questionIndex} for room {roomCode}");
    }

    /// <summary>
    /// Gửi message đến tất cả client trong phòng
    /// </summary>
    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var message = JsonSerializer.Serialize(new {
            eventName = eventName,
            data = data
        });
        var buffer = Encoding.UTF8.GetBytes(message);
        
        var sendTasks = gameRoom.Players
            .Where(p => !string.IsNullOrEmpty(p.SocketId))
            .Select(async player =>
            {
                if (_connections.TryGetValue(player.SocketId!, out var socket) && 
                    socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[HOST] Failed to send message to {player.Username}: {ex.Message}");
                    }
                }
            });
        
        await Task.WhenAll(sendTasks);
    }

    /// <summary>
    /// Gửi message đến một player cụ thể
    /// </summary>
    private async Task SendToPlayerAsync(string roomCode, string username, string eventName, object data)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
        if (player?.SocketId == null) return;
        
        if (_connections.TryGetValue(player.SocketId, out var socket) && socket.State == WebSocketState.Open)
        {
            try
            {
                var message = JsonSerializer.Serialize(new {
                    eventName = eventName,
                    data = data
                });
                var buffer = Encoding.UTF8.GetBytes(message);
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HOST] Failed to send message to {username}: {ex.Message}");
            }
        }
    }

    #endregion
}