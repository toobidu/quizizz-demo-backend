using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service xử lý tương tác người chơi qua WebSocket - Chịu trách nhiệm:
/// 1. Nhận và xử lý câu trả lời từ người chơi
/// 2. Cập nhật trạng thái người chơi (online, offline, answering)
/// 3. Tính điểm realtime
/// 4. Xử lý các tương tác khác (chat, emoji, etc.)
/// </summary>
public class PlayerInteractionSocketServiceImplement : IPlayerInteractionSocketService
{
    // Dictionary lưu trữ các phòng game (shared với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (shared với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    
    // Dictionary lưu trữ game sessions (shared với GameFlowService)
    private readonly ConcurrentDictionary<string, PlayerGameSession> _gameSessions = new();

    /// <summary>
    /// Class nội bộ để quản lý session game của phòng
    /// </summary>
    private class PlayerGameSession
    {
        public string RoomCode { get; set; } = string.Empty;
        public List<QuestionData> Questions { get; set; } = new();
        public Dictionary<string, PlayerGameResult> PlayerResults { get; set; } = new();
        public bool IsGameActive { get; set; } = false;
        public DateTime GameStartTime { get; set; }
        public int GameTimeLimit { get; set; } = 300;
    }

    /// <summary>
    /// Class lưu trữ kết quả game của từng người chơi
    /// </summary>
    private class PlayerGameResult
    {
        public string Username { get; set; } = string.Empty;
        public List<PlayerAnswer> Answers { get; set; } = new();
        public int Score { get; set; } = 0;
        public DateTime? LastAnswerTime { get; set; }
        public string Status { get; set; } = "waiting"; // waiting, answering, answered, finished
    }

    /// <summary>
    /// Nhận và xử lý câu trả lời từ người chơi
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi</param>
    /// <param name="answer">Câu trả lời (JSON object chứa questionId, selectedAnswer, etc.)</param>
    /// <param name="timestamp">Thời gian submit answer (milliseconds)</param>
    public async Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp)
    {
        Console.WriteLine($"[PLAYER] Received answer from {username} in room {roomCode} at {timestamp}");
        
        try
        {
            // Validate game session tồn tại
            if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                Console.WriteLine($"[PLAYER] No active game session for room {roomCode}");
                await SendErrorToPlayerAsync(roomCode, username, "Không có game nào đang diễn ra");
                return;
            }

            // Validate game đang active
            if (!gameSession.IsGameActive)
            {
                Console.WriteLine($"[PLAYER] Game is not active for room {roomCode}");
                await SendErrorToPlayerAsync(roomCode, username, "Game đã kết thúc");
                return;
            }

            // Parse câu trả lời từ JSON
            var answerJson = JsonSerializer.Serialize(answer);
            var playerAnswer = JsonSerializer.Deserialize<PlayerAnswerSubmission>(answerJson);
            
            if (playerAnswer == null)
            {
                Console.WriteLine($"[PLAYER] Invalid answer format from {username}");
                await SendErrorToPlayerAsync(roomCode, username, "Định dạng câu trả lời không hợp lệ");
                return;
            }

            // Validate câu hỏi có hợp lệ không
            if (playerAnswer.QuestionIndex < 0 || playerAnswer.QuestionIndex >= gameSession.Questions.Count)
            {
                Console.WriteLine($"[PLAYER] Invalid question index {playerAnswer.QuestionIndex} from {username}");
                await SendErrorToPlayerAsync(roomCode, username, "Câu hỏi không hợp lệ");
                return;
            }

            // Lấy thông tin câu hỏi
            var question = gameSession.Questions[playerAnswer.QuestionIndex];
            
            // Kiểm tra xem player đã trả lời câu này chưa
            if (!gameSession.PlayerResults.TryGetValue(username, out var playerResult))
            {
                playerResult = new PlayerGameResult { Username = username };
                gameSession.PlayerResults[username] = playerResult;
            }

            // Kiểm tra duplicate answer
            var existingAnswer = playerResult.Answers.FirstOrDefault(a => a.QuestionIndex == playerAnswer.QuestionIndex);
            if (existingAnswer != null)
            {
                Console.WriteLine($"[PLAYER] {username} already answered question {playerAnswer.QuestionIndex}");
                await SendErrorToPlayerAsync(roomCode, username, "Bạn đã trả lời câu hỏi này rồi");
                return;
            }

            // Tính điểm dựa trên độ chính xác và thời gian
            var isCorrect = IsAnswerCorrect(question, playerAnswer.SelectedAnswer);
            var timeToAnswer = CalculateTimeToAnswer(gameSession.GameStartTime, timestamp);
            var pointsEarned = CalculatePoints(isCorrect, timeToAnswer, question);

            // Tạo PlayerAnswer object
            var answerRecord = new PlayerAnswer
            {
                Username = username,
                Answer = playerAnswer.SelectedAnswer,
                Timestamp = timestamp,
                TimeToAnswer = timeToAnswer,
                IsCorrect = isCorrect,
                PointsEarned = pointsEarned,
                QuestionIndex = playerAnswer.QuestionIndex
            };

            // Lưu câu trả lời vào player result
            playerResult.Answers.Add(answerRecord);
            playerResult.Score += pointsEarned;
            playerResult.LastAnswerTime = DateTime.UtcNow;
            playerResult.Status = "answered";

            Console.WriteLine($"[PLAYER] {username} answered question {playerAnswer.QuestionIndex}: {(isCorrect ? "CORRECT" : "WRONG")} (+{pointsEarned} points)");

            // Gửi feedback cho player
            await SendToPlayerAsync(roomCode, username, "answer-result", new {
                questionIndex = playerAnswer.QuestionIndex,
                isCorrect = isCorrect,
                correctAnswer = question.CorrectAnswer,
                pointsEarned = pointsEarned,
                totalScore = playerResult.Score,
                timeToAnswer = timeToAnswer
            });

            // Broadcast cập nhật điểm số realtime (không tiết lộ đáp án đúng)
            await BroadcastScoreUpdateAsync(roomCode);

            // Kiểm tra xem tất cả player đã trả lời câu này chưa
            await CheckQuestionCompletionAsync(roomCode, playerAnswer.QuestionIndex);

            // Kiểm tra xem player đã hoàn thành tất cả câu hỏi chưa
            if (playerResult.Answers.Count >= gameSession.Questions.Count)
            {
                playerResult.Status = "finished";
                await SendToPlayerAsync(roomCode, username, "player-finished", new {
                    message = "Bạn đã hoàn thành tất cả câu hỏi!",
                    finalScore = playerResult.Score,
                    totalQuestions = gameSession.Questions.Count
                });

                // Kiểm tra xem tất cả player đã hoàn thành chưa
                await CheckAllPlayersFinishedAsync(roomCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Error processing answer from {username}: {ex.Message}");
            await SendErrorToPlayerAsync(roomCode, username, "Lỗi xử lý câu trả lời");
        }
    }

    /// <summary>
    /// Cập nhật trạng thái của người chơi
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi</param>
    /// <param name="status">Trạng thái mới (online, offline, answering, waiting, finished)</param>
    public async Task UpdatePlayerStatusAsync(string roomCode, string username, string status)
    {
        Console.WriteLine($"[PLAYER] Updating status for {username} in room {roomCode} to: {status}");
        
        try
        {
            // Cập nhật trạng thái player trong game session
            if (_gameSessions.TryGetValue(roomCode, out var gameSession))
            {
                if (gameSession.PlayerResults.TryGetValue(username, out var playerResult))
                {
                    var oldStatus = playerResult.Status;
                    playerResult.Status = status;
                    
                    Console.WriteLine($"[PLAYER] {username} status changed from {oldStatus} to {status}");
                }
            }

            // Cập nhật trạng thái trong game room
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
                if (player != null)
                {
                    player.Status = status;
                }
            }

            // Broadcast trạng thái mới đến tất cả client khác (không gửi cho chính player đó)
            await BroadcastPlayerStatusAsync(roomCode, username, status);

            // Xử lý logic tương ứng với trạng thái
            switch (status.ToLower())
            {
                case "online":
                    Console.WriteLine($"[PLAYER] {username} is now online in room {roomCode}");
                    break;
                    
                case "offline":
                    Console.WriteLine($"[PLAYER] {username} went offline in room {roomCode}");
                    // Có thể pause timer cho player này nếu cần
                    break;
                    
                case "answering":
                    Console.WriteLine($"[PLAYER] {username} is answering a question in room {roomCode}");
                    break;
                    
                case "waiting":
                    Console.WriteLine($"[PLAYER] {username} is waiting for next question in room {roomCode}");
                    break;
                    
                case "finished":
                    Console.WriteLine($"[PLAYER] {username} has finished the game in room {roomCode}");
                    await CheckAllPlayersFinishedAsync(roomCode);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PLAYER] Error updating status for {username}: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Class để parse câu trả lời từ client
    /// </summary>
    private class PlayerAnswerSubmission
    {
        public int QuestionIndex { get; set; }
        public object SelectedAnswer { get; set; } = new();
        public long SubmitTime { get; set; }
    }

    /// <summary>
    /// Kiểm tra câu trả lời có đúng không
    /// </summary>
    private bool IsAnswerCorrect(QuestionData question, object selectedAnswer)
    {
        try
        {
            var selectedStr = selectedAnswer.ToString()?.Trim().ToLower();
            var correctStr = question.CorrectAnswer.Trim().ToLower();
            
            return selectedStr == correctStr;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tính thời gian trả lời (giây)
    /// </summary>
    private int CalculateTimeToAnswer(DateTime gameStartTime, long submitTimestamp)
    {
        try
        {
            var submitTime = DateTimeOffset.FromUnixTimeMilliseconds(submitTimestamp).DateTime;
            var timeToAnswer = (submitTime - gameStartTime).TotalSeconds;
            return Math.Max(1, (int)timeToAnswer); // Tối thiểu 1 giây
        }
        catch
        {
            return 30; // Default 30 giây nếu không tính được
        }
    }

    /// <summary>
    /// Tính điểm dựa trên độ chính xác và thời gian
    /// </summary>
    private int CalculatePoints(bool isCorrect, int timeToAnswer, QuestionData question)
    {
        if (!isCorrect) return 0;
        
        // Điểm cơ bản: 100 điểm cho câu đúng
        var basePoints = 100;
        
        // Bonus điểm dựa trên tốc độ (càng nhanh càng nhiều điểm)
        // Giả sử thời gian tối đa cho 1 câu là 30 giây
        var maxTime = 30;
        var speedBonus = Math.Max(0, (maxTime - timeToAnswer) * 2); // 2 điểm/giây tiết kiệm được
        
        var totalPoints = basePoints + speedBonus;
        
        Console.WriteLine($"[SCORING] Question answered in {timeToAnswer}s: {basePoints} base + {speedBonus} speed bonus = {totalPoints} points");
        
        return totalPoints;
    }

    /// <summary>
    /// Gửi lỗi cho một player cụ thể
    /// </summary>
    private async Task SendErrorToPlayerAsync(string roomCode, string username, string errorMessage)
    {
        await SendToPlayerAsync(roomCode, username, "error", new {
            message = errorMessage,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast cập nhật điểm số cho tất cả player
    /// </summary>
    private async Task BroadcastScoreUpdateAsync(string roomCode)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession)) return;
        
        var scoreboard = gameSession.PlayerResults.Values
            .Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count,
                status = p.Status
            })
            .OrderByDescending(p => p.score)
            .ToList();

        await BroadcastToRoomAsync(roomCode, "scoreboard-update", new {
            scoreboard = scoreboard,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Broadcast trạng thái player
    /// </summary>
    private async Task BroadcastPlayerStatusAsync(string roomCode, string username, string status)
    {
        await BroadcastToRoomAsync(roomCode, "player-status-changed", new {
            username = username,
            status = status,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Kiểm tra xem tất cả player đã trả lời câu hỏi hiện tại chưa
    /// </summary>
    private async Task CheckQuestionCompletionAsync(string roomCode, int questionIndex)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession)) return;
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var totalPlayers = gameRoom.Players.Count;
        var answeredPlayers = gameSession.PlayerResults.Values
            .Count(p => p.Answers.Any(a => a.QuestionIndex == questionIndex));
        
        Console.WriteLine($"[PLAYER] Question {questionIndex}: {answeredPlayers}/{totalPlayers} players answered");
        
        if (answeredPlayers >= totalPlayers)
        {
            Console.WriteLine($"[PLAYER] All players answered question {questionIndex} in room {roomCode}");
            
            // Có thể tự động chuyển câu hỏi tiếp theo hoặc thông báo cho host
            await BroadcastToRoomAsync(roomCode, "question-completed", new {
                questionIndex = questionIndex,
                message = "Tất cả người chơi đã trả lời câu hỏi này"
            });
        }
    }

    /// <summary>
    /// Kiểm tra xem tất cả player đã hoàn thành game chưa
    /// </summary>
    private async Task CheckAllPlayersFinishedAsync(string roomCode)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession)) return;
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom)) return;
        
        var totalPlayers = gameRoom.Players.Count;
        var finishedPlayers = gameSession.PlayerResults.Values.Count(p => p.Status == "finished");
        
        Console.WriteLine($"[PLAYER] Game progress: {finishedPlayers}/{totalPlayers} players finished");
        
        if (finishedPlayers >= totalPlayers && gameSession.IsGameActive)
        {
            Console.WriteLine($"[PLAYER] All players finished game in room {roomCode}");
            
            gameSession.IsGameActive = false;
            
            // Tạo final results
            var finalResults = gameSession.PlayerResults.Values
                .Select(p => new {
                    username = p.Username,
                    score = p.Score,
                    answersCount = p.Answers.Count,
                    correctAnswers = p.Answers.Count(a => a.IsCorrect),
                    averageTime = p.Answers.Count > 0 ? p.Answers.Average(a => a.TimeToAnswer) : 0
                })
                .OrderByDescending(p => p.score)
                .ToList();

            await BroadcastToRoomAsync(roomCode, "game-completed", new {
                reason = "all-finished",
                message = "Tất cả người chơi đã hoàn thành!",
                finalResults = finalResults
            });
        }
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
                        Console.WriteLine($"[PLAYER] Failed to send message to {player.Username}: {ex.Message}");
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
                Console.WriteLine($"[PLAYER] Failed to send message to {username}: {ex.Message}");
            }
        }
    }

    #endregion
}