using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service quản lý luồng game qua WebSocket - Chịu trách nhiệm:
/// 1. Bắt đầu game và gửi câu hỏi
/// 2. Quản lý timer game và countdown
/// 3. Theo dõi tiến độ người chơi
/// 4. Gửi câu hỏi tiếp theo
/// 5. Kết thúc game và dọn dẹp session
/// </summary>
public class GameFlowSocketServiceImplement : IGameFlowSocketService
{
    // Dictionary lưu trữ các game session đang hoạt động
    // Key: roomCode, Value: GameSession object
    private readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();
    
    // Dictionary lưu trữ các phòng game (shared với RoomManagementService)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (shared với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    /// <summary>
    /// Class nội bộ để quản lý session của một game
    /// </summary>
    private class GameSession
    {
        public string RoomCode { get; set; } = string.Empty;
        public List<QuestionData> Questions { get; set; } = new();
        public int CurrentQuestionIndex { get; set; } = 0;
        public int GameTimeLimit { get; set; } = 300; // 5 phút mặc định
        public DateTime GameStartTime { get; set; }
        public bool IsGameActive { get; set; } = false;
        public bool IsGameEnded { get; set; } = false;
        public Timer? GameTimer { get; set; }
        public Timer? CountdownTimer { get; set; }
        public Dictionary<string, PlayerGameProgress> PlayerProgress { get; set; } = new();
    }

    /// <summary>
    /// Class theo dõi tiến độ của từng người chơi
    /// </summary>
    private class PlayerGameProgress
    {
        public string Username { get; set; } = string.Empty;
        public int CurrentQuestionIndex { get; set; } = 0;
        public int Score { get; set; } = 0;
        public List<PlayerAnswer> Answers { get; set; } = new();
        public bool HasFinished { get; set; } = false;
        public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Bắt đầu game trong phòng (version đơn giản)
    /// </summary>
    /// <param name="roomCode">Mã phòng cần bắt đầu game</param>
    public async Task StartGameAsync(string roomCode)
    {
        Console.WriteLine($"[GAME] Starting simple game for room: {roomCode}");
        
        // Kiểm tra phòng có tồn tại không
        if (!_gameRooms.ContainsKey(roomCode))
        {
            Console.WriteLine($"[GAME] Room {roomCode} not found");
            return;
        }

        var gameRoom = _gameRooms[roomCode];
        
        // Kiểm tra có đủ người chơi không (tối thiểu 1 người)
        if (gameRoom.Players.Count == 0)
        {
            Console.WriteLine($"[GAME] Room {roomCode} has no players");
            return;
        }

        // Tạo game session mới
        var gameSession = new GameSession
        {
            RoomCode = roomCode,
            GameStartTime = DateTime.UtcNow,
            IsGameActive = true
        };
        
        _gameSessions[roomCode] = gameSession;

        // Thay đổi trạng thái phòng thành "playing"
        gameRoom.GameState = "playing";
        
        // Broadcast thông báo game bắt đầu
        await BroadcastToRoomAsync(roomCode, "game-started", new {
            message = "Game đã bắt đầu!",
            startTime = gameSession.GameStartTime,
            roomCode = roomCode
        });

        Console.WriteLine($"[GAME] Simple game started for room {roomCode} with {gameRoom.Players.Count} players");
    }

    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="questions">Danh sách câu hỏi (JSON object)</param>
    /// <param name="gameTimeLimit">Thời gian giới hạn cho toàn bộ game (giây)</param>
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit)
    {
        Console.WriteLine($"[GAME] Starting game with questions for room: {roomCode}, time limit: {gameTimeLimit}s");
        
        try
        {
            // Parse danh sách câu hỏi từ JSON
            var questionsJson = JsonSerializer.Serialize(questions);
            var questionList = JsonSerializer.Deserialize<List<QuestionData>>(questionsJson) ?? new List<QuestionData>();
            
            if (questionList.Count == 0)
            {
                Console.WriteLine($"[GAME] No questions provided for room {roomCode}");
                return;
            }

            // Tạo game session với câu hỏi
            var gameSession = new GameSession
            {
                RoomCode = roomCode,
                Questions = questionList,
                GameTimeLimit = gameTimeLimit,
                GameStartTime = DateTime.UtcNow,
                IsGameActive = true
            };

            // Khởi tạo progress cho tất cả player
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                foreach (var player in gameRoom.Players)
                {
                    gameSession.PlayerProgress[player.Username] = new PlayerGameProgress
                    {
                        Username = player.Username
                    };
                }
            }

            _gameSessions[roomCode] = gameSession;

            // Gửi countdown 3-2-1 trước khi bắt đầu
            await SendCountdownAsync(roomCode, 3);
            
            // Khởi tạo timer cho game (tự động kết thúc khi hết thời gian)
            gameSession.GameTimer = new Timer(async _ =>
            {
                await EndGameDueToTimeoutAsync(roomCode);
            }, null, TimeSpan.FromSeconds(gameTimeLimit), Timeout.InfiniteTimeSpan);

            Console.WriteLine($"[GAME] Game with {questionList.Count} questions started for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error starting game with questions for room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi câu hỏi tiếp theo cho một người chơi cụ thể
    /// Dùng trong chế độ self-paced (mỗi người chơi với tốc độ riêng)
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi cần nhận câu hỏi</param>
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAME] Sending next question to player {username} in room {roomCode}");
        
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            Console.WriteLine($"[GAME] No active game session for room {roomCode}");
            return;
        }

        if (!gameSession.PlayerProgress.TryGetValue(username, out var playerProgress))
        {
            Console.WriteLine($"[GAME] Player {username} not found in game session");
            return;
        }

        // Kiểm tra xem còn câu hỏi nào không
        if (playerProgress.CurrentQuestionIndex >= gameSession.Questions.Count)
        {
            Console.WriteLine($"[GAME] Player {username} has finished all questions");
            playerProgress.HasFinished = true;
            
            // Thông báo player đã hoàn thành
            await SendToPlayerAsync(roomCode, username, "player-finished", new {
                message = "Bạn đã hoàn thành tất cả câu hỏi!",
                finalScore = playerProgress.Score
            });
            
            // Kiểm tra xem tất cả player đã hoàn thành chưa
            await CheckAllPlayersFinishedAsync(roomCode);
            return;
        }

        // Lấy câu hỏi tiếp theo
        var nextQuestion = gameSession.Questions[playerProgress.CurrentQuestionIndex];
        
        // Gửi câu hỏi cho player
        await SendToPlayerAsync(roomCode, username, "next-question", new {
            question = nextQuestion,
            questionIndex = playerProgress.CurrentQuestionIndex,
            totalQuestions = gameSession.Questions.Count,
            timeRemaining = GetGameTimeRemaining(gameSession)
        });

        // Cập nhật thời gian hoạt động cuối
        playerProgress.LastActivityTime = DateTime.UtcNow;
        
        Console.WriteLine($"[GAME] Sent question {playerProgress.CurrentQuestionIndex + 1}/{gameSession.Questions.Count} to {username}");
    }

    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi trong phòng
    /// Dùng trong chế độ synchronized (tất cả cùng câu hỏi)
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="question">Câu hỏi cần gửi (JSON object)</param>
    /// <param name="questionIndex">Thứ tự câu hỏi (bắt đầu từ 0)</param>
    /// <param name="totalQuestions">Tổng số câu hỏi</param>
    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions)
    {
        Console.WriteLine($"[GAME] Sending question {questionIndex + 1}/{totalQuestions} to room {roomCode}");
        
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            Console.WriteLine($"[GAME] No active game session for room {roomCode}");
            return;
        }

        // Cập nhật current question index của game session
        gameSession.CurrentQuestionIndex = questionIndex;

        // Broadcast câu hỏi đến tất cả player trong phòng
        await BroadcastToRoomAsync(roomCode, "new-question", new {
            question = question,
            questionIndex = questionIndex,
            totalQuestions = totalQuestions,
            timeRemaining = GetGameTimeRemaining(gameSession),
            gameState = "question-active"
        });

        Console.WriteLine($"[GAME] Question {questionIndex + 1}/{totalQuestions} broadcasted to room {roomCode}");
    }

    /// <summary>
    /// Gửi cập nhật thời gian game còn lại
    /// Được gọi định kỳ bởi timer
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task SendGameTimerUpdateAsync(string roomCode)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            return;
        }

        var timeRemaining = GetGameTimeRemaining(gameSession);
        
        // Broadcast thời gian còn lại đến tất cả client
        await BroadcastToRoomAsync(roomCode, "timer-update", new {
            timeRemaining = timeRemaining,
            totalTime = gameSession.GameTimeLimit,
            gameState = gameSession.IsGameActive ? "playing" : "ended"
        });

        // Nếu hết thời gian thì kết thúc game
        if (timeRemaining <= 0 && gameSession.IsGameActive)
        {
            await EndGameDueToTimeoutAsync(roomCode);
        }
    }

    /// <summary>
    /// Lấy tiến độ của một người chơi cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi</param>
    public async Task GetPlayerProgressAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAME] Getting progress for player {username} in room {roomCode}");
        
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            return;
        }

        if (!gameSession.PlayerProgress.TryGetValue(username, out var playerProgress))
        {
            return;
        }

        // Gửi thông tin tiến độ cho player
        await SendToPlayerAsync(roomCode, username, "player-progress", new {
            currentQuestionIndex = playerProgress.CurrentQuestionIndex,
            totalQuestions = gameSession.Questions.Count,
            score = playerProgress.Score,
            answersCount = playerProgress.Answers.Count,
            hasFinished = playerProgress.HasFinished,
            timeRemaining = GetGameTimeRemaining(gameSession)
        });
    }

    /// <summary>
    /// Broadcast tiến độ của tất cả người chơi
    /// Để hiển thị realtime leaderboard
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task BroadcastPlayerProgressAsync(string roomCode)
    {
        Console.WriteLine($"[GAME] Broadcasting player progress for room {roomCode}");
        
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            return;
        }

        // Thu thập tiến độ của tất cả player
        var progressList = gameSession.PlayerProgress.Values
            .Select(p => new {
                username = p.Username,
                score = p.Score,
                currentQuestion = p.CurrentQuestionIndex + 1,
                totalQuestions = gameSession.Questions.Count,
                hasFinished = p.HasFinished,
                answersCount = p.Answers.Count
            })
            .OrderByDescending(p => p.score) // Sắp xếp theo điểm số
            .ToList();

        // Broadcast leaderboard realtime
        await BroadcastToRoomAsync(roomCode, "progress-update", new {
            players = progressList,
            gameState = gameSession.IsGameActive ? "playing" : "ended",
            timeRemaining = GetGameTimeRemaining(gameSession)
        });
    }

    /// <summary>
    /// Dọn dẹp game session khi kết thúc
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task CleanupGameSessionAsync(string roomCode)
    {
        Console.WriteLine($"[GAME] Cleaning up game session for room {roomCode}");
        
        if (_gameSessions.TryRemove(roomCode, out var gameSession))
        {
            // Dừng tất cả timer
            gameSession.GameTimer?.Dispose();
            gameSession.CountdownTimer?.Dispose();
            
            // Reset trạng thái phòng về "waiting"
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                gameRoom.GameState = "lobby";
                gameRoom.CurrentQuestionIndex = 0;
            }

            Console.WriteLine($"[GAME] Game session cleaned up for room {roomCode}");
        }
    }

    /// <summary>
    /// Cập nhật trạng thái game
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="gameState">Trạng thái mới (waiting, countdown, playing, ended)</param>
    public async Task UpdateGameStateAsync(string roomCode, string gameState)
    {
        Console.WriteLine($"[GAME] Updating game state for room {roomCode} to: {gameState}");
        
        // Cập nhật trạng thái trong game session
        if (_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            gameSession.IsGameActive = gameState == "playing";
            if (gameState == "ended")
            {
                gameSession.IsGameEnded = true;
            }
        }

        // Cập nhật trạng thái trong game room
        if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
        {
            gameRoom.GameState = gameState;
        }

        // Broadcast trạng thái mới đến tất cả client
        await BroadcastToRoomAsync(roomCode, "game-state-changed", new {
            gameState = gameState,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="countdown">Số giây đếm ngược (3, 2, 1, 0)</param>
    public async Task SendCountdownAsync(string roomCode, int countdown)
    {
        Console.WriteLine($"[GAME] Sending countdown {countdown} for room {roomCode}");
        
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            return;
        }

        // Tạo countdown timer
        var currentCount = countdown;
        gameSession.CountdownTimer = new Timer(async _ =>
        {
            if (currentCount > 0)
            {
                // Broadcast số đếm ngược
                await BroadcastToRoomAsync(roomCode, "countdown", new {
                    count = currentCount,
                    message = currentCount.ToString()
                });
                currentCount--;
            }
            else
            {
                // Khi countdown = 0 thì bắt đầu game thực sự
                await BroadcastToRoomAsync(roomCode, "countdown", new {
                    count = 0,
                    message = "Bắt đầu!"
                });
                
                // Gửi câu hỏi đầu tiên nếu có
                if (gameSession.Questions.Count > 0)
                {
                    await SendQuestionAsync(roomCode, gameSession.Questions[0], 0, gameSession.Questions.Count);
                }
                
                // Dừng countdown timer
                gameSession.CountdownTimer?.Dispose();
                gameSession.CountdownTimer = null;
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    #region Private Helper Methods

    /// <summary>
    /// Tính thời gian còn lại của game (giây)
    /// </summary>
    private int GetGameTimeRemaining(GameSession gameSession)
    {
        if (!gameSession.IsGameActive) return 0;
        
        var elapsed = (DateTime.UtcNow - gameSession.GameStartTime).TotalSeconds;
        var remaining = gameSession.GameTimeLimit - elapsed;
        return Math.Max(0, (int)remaining);
    }

    /// <summary>
    /// Kết thúc game do hết thời gian
    /// </summary>
    private async Task EndGameDueToTimeoutAsync(string roomCode)
    {
        Console.WriteLine($"[GAME] Game ended due to timeout for room {roomCode}");
        
        if (_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            gameSession.IsGameActive = false;
            gameSession.IsGameEnded = true;
            
            await BroadcastToRoomAsync(roomCode, "game-ended", new {
                reason = "timeout",
                message = "Game đã kết thúc do hết thời gian!",
                finalResults = gameSession.PlayerProgress.Values.Select(p => new {
                    username = p.Username,
                    score = p.Score,
                    answersCount = p.Answers.Count
                }).OrderByDescending(p => p.score).ToList()
            });
            
            // Cleanup sau 10 giây
            _ = Task.Delay(10000).ContinueWith(async _ => await CleanupGameSessionAsync(roomCode));
        }
    }

    /// <summary>
    /// Kiểm tra xem tất cả player đã hoàn thành chưa
    /// </summary>
    private async Task CheckAllPlayersFinishedAsync(string roomCode)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            return;
        }

        var allFinished = gameSession.PlayerProgress.Values.All(p => p.HasFinished);
        if (allFinished && gameSession.IsGameActive)
        {
            Console.WriteLine($"[GAME] All players finished for room {roomCode}");
            
            gameSession.IsGameActive = false;
            gameSession.IsGameEnded = true;
            
            await BroadcastToRoomAsync(roomCode, "game-ended", new {
                reason = "all-finished",
                message = "Tất cả người chơi đã hoàn thành!",
                finalResults = gameSession.PlayerProgress.Values.Select(p => new {
                    username = p.Username,
                    score = p.Score,
                    answersCount = p.Answers.Count
                }).OrderByDescending(p => p.score).ToList()
            });
            
            // Cleanup sau 10 giây
            _ = Task.Delay(10000).ContinueWith(async _ => await CleanupGameSessionAsync(roomCode));
        }
    }

    /// <summary>
    /// Gửi message đến tất cả client trong một phòng
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
                        Console.WriteLine($"[GAME] Failed to send message to {player.Username}: {ex.Message}");
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
                Console.WriteLine($"[GAME] Failed to send message to {username}: {ex.Message}");
            }
        }
    }

    #endregion
}