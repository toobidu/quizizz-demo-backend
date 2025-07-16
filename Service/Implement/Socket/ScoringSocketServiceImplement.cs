using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service xử lý tính điểm và bảng xếp hạng qua WebSocket - Chịu trách nhiệm:
/// 1. Tính điểm realtime cho từng câu trả lời
/// 2. Cập nhật và broadcast bảng điểm
/// 3. Gửi kết quả cuối game
/// 4. Xử lý các loại điểm khác nhau (accuracy, speed bonus, streak bonus)
/// </summary>
public class ScoringSocketServiceImplement : IScoringSocketService
{
    // Dictionary lưu trữ các phòng game (shared với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (shared với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    
    // Dictionary lưu trữ scoring sessions
    private readonly ConcurrentDictionary<string, ScoringSession> _scoringSessions = new();

    /// <summary>
    /// Class quản lý scoring session của một phòng
    /// </summary>
    private class ScoringSession
    {
        public string RoomCode { get; set; } = string.Empty;
        public Dictionary<string, PlayerScore> PlayerScores { get; set; } = new();
        public List<ScoreboardEntry> CurrentScoreboard { get; set; } = new();
        public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
        public bool IsGameActive { get; set; } = true;
    }

    /// <summary>
    /// Class lưu trữ điểm số chi tiết của từng player
    /// </summary>
    private class PlayerScore
    {
        public string Username { get; set; } = string.Empty;
        public int TotalScore { get; set; } = 0;
        public int CorrectAnswers { get; set; } = 0;
        public int TotalAnswers { get; set; } = 0;
        public double AverageTime { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0; // Chuỗi trả lời đúng liên tiếp
        public int MaxStreak { get; set; } = 0; // Chuỗi dài nhất
        public List<int> QuestionScores { get; set; } = new(); // Điểm từng câu
        public DateTime LastAnswerTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Cập nhật bảng điểm realtime
    /// Được gọi sau mỗi câu trả lời hoặc định kỳ
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="scoreboard">Bảng điểm hiện tại (JSON object)</param>
    public async Task UpdateScoreboardAsync(string roomCode, object scoreboard)
    {
        Console.WriteLine($"[SCORING] Updating scoreboard for room {roomCode}");
        
        try
        {
            // Parse scoreboard data từ input
            var scoreboardJson = JsonSerializer.Serialize(scoreboard);
            var scoreboardData = JsonSerializer.Deserialize<List<ScoreboardUpdateData>>(scoreboardJson);
            
            if (scoreboardData == null || scoreboardData.Count == 0)
            {
                Console.WriteLine($"[SCORING] No scoreboard data provided for room {roomCode}");
                return;
            }

            // Lấy hoặc tạo scoring session
            if (!_scoringSessions.TryGetValue(roomCode, out var scoringSession))
            {
                scoringSession = new ScoringSession { RoomCode = roomCode };
                _scoringSessions[roomCode] = scoringSession;
            }

            // Cập nhật điểm số cho từng player
            foreach (var playerData in scoreboardData)
            {
                if (!scoringSession.PlayerScores.TryGetValue(playerData.Username, out var playerScore))
                {
                    playerScore = new PlayerScore { Username = playerData.Username };
                    scoringSession.PlayerScores[playerData.Username] = playerScore;
                }

                // Cập nhật thông tin điểm số
                playerScore.TotalScore = playerData.Score;
                playerScore.CorrectAnswers = playerData.CorrectAnswers;
                playerScore.TotalAnswers = playerData.TotalAnswers;
                playerScore.AverageTime = playerData.AverageTime;
                playerScore.LastAnswerTime = DateTime.UtcNow;
            }

            // Tính toán bảng xếp hạng mới
            var newScoreboard = CalculateScoreboard(scoringSession);
            
            // Detect thay đổi vị trí (ai lên/xuống hạng)
            var positionChanges = DetectPositionChanges(scoringSession.CurrentScoreboard, newScoreboard);
            
            // Cập nhật scoreboard hiện tại
            scoringSession.CurrentScoreboard = newScoreboard;
            scoringSession.LastUpdateTime = DateTime.UtcNow;

            // Broadcast bảng điểm realtime đến tất cả client
            await BroadcastToRoomAsync(roomCode, "scoreboard-updated", new {
                scoreboard = newScoreboard,
                positionChanges = positionChanges,
                timestamp = DateTime.UtcNow,
                totalPlayers = newScoreboard.Count
            });

            Console.WriteLine($"[SCORING] Scoreboard updated for room {roomCode}: {newScoreboard.Count} players");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCORING] Error updating scoreboard for room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi kết quả cuối game đến tất cả người chơi
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="finalResults">Kết quả cuối game (JSON object)</param>
    public async Task SendFinalResultsAsync(string roomCode, object finalResults)
    {
        Console.WriteLine($"[SCORING] Sending final results for room {roomCode}");
        
        try
        {
            if (!_scoringSessions.TryGetValue(roomCode, out var scoringSession))
            {
                Console.WriteLine($"[SCORING] No scoring session found for room {roomCode}");
                return;
            }

            // Tính toán kết quả cuối cùng chi tiết
            var detailedResults = CalculateFinalResults(scoringSession);
            
            // Parse final results từ input (nếu có)
            var inputResultsJson = JsonSerializer.Serialize(finalResults);
            var inputResults = JsonSerializer.Deserialize<Dictionary<string, object>>(inputResultsJson);

            // Merge với detailed results
            var combinedResults = new {
                // Kết quả chi tiết từ scoring session
                rankings = detailedResults.Rankings,
                statistics = detailedResults.Statistics,
                achievements = detailedResults.Achievements,
                
                // Thông tin từ input (nếu có)
                gameInfo = inputResults,
                
                // Metadata
                gameEndTime = DateTime.UtcNow,
                totalDuration = (DateTime.UtcNow - detailedResults.GameStartTime).TotalMinutes,
                roomCode = roomCode
            };

            // Broadcast kết quả cuối đến tất cả client
            await BroadcastToRoomAsync(roomCode, "final-results", combinedResults);

            // Gửi kết quả cá nhân cho từng player
            foreach (var playerScore in scoringSession.PlayerScores.Values)
            {
                var personalResult = CreatePersonalResult(playerScore, detailedResults);
                await SendToPlayerAsync(roomCode, playerScore.Username, "personal-final-result", personalResult);
            }

            // Đánh dấu game đã kết thúc
            scoringSession.IsGameActive = false;
            
            Console.WriteLine($"[SCORING] Final results sent for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCORING] Error sending final results for room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Kết thúc game và gửi kết quả final
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="finalResults">Kết quả cuối game</param>
    public async Task EndGameAsync(string roomCode, object finalResults)
    {
        Console.WriteLine($"[SCORING] Ending game for room {roomCode}");
        
        try
        {
            // Dừng tất cả timer và game logic (nếu có)
            if (_scoringSessions.TryGetValue(roomCode, out var scoringSession))
            {
                scoringSession.IsGameActive = false;
            }

            // Tính toán kết quả cuối cùng
            await SendFinalResultsAsync(roomCode, finalResults);

            // Chuyển trạng thái phòng về "ended"
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                gameRoom.GameState = "finished";
            }

            // Broadcast thông báo game kết thúc
            await BroadcastToRoomAsync(roomCode, "game-ended", new {
                message = "Game đã kết thúc!",
                finalResults = finalResults,
                timestamp = DateTime.UtcNow,
                nextActions = new {
                    canStartNewGame = true,
                    canLeaveRoom = true,
                    showResults = true
                }
            });

            // Cleanup scoring session sau 5 phút
            _ = Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ => {
                _scoringSessions.TryRemove(roomCode, out var _);
                Console.WriteLine($"[SCORING] Scoring session cleaned up for room {roomCode}");
            });

            Console.WriteLine($"[SCORING] Game ended for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCORING] Error ending game for room {roomCode}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi bảng điểm hiện tại
    /// Khác với UpdateScoreboardAsync - method này chỉ gửi, không tính toán
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="scoreboard">Bảng điểm cần gửi</param>
    public async Task SendScoreboardAsync(string roomCode, object scoreboard)
    {
        Console.WriteLine($"[SCORING] Sending scoreboard for room {roomCode}");
        
        try
        {
            // Format bảng điểm theo định dạng client mong đợi
            var formattedScoreboard = FormatScoreboardForClient(scoreboard);

            // Gửi cho tất cả client trong phòng
            await BroadcastToRoomAsync(roomCode, "scoreboard", new {
                scoreboard = formattedScoreboard,
                timestamp = DateTime.UtcNow,
                type = "current"
            });

            // Có thể gửi cho từng client khác nhau (ví dụ: highlight vị trí của chính họ)
            if (_scoringSessions.TryGetValue(roomCode, out var scoringSession))
            {
                foreach (var playerScore in scoringSession.PlayerScores.Values)
                {
                    var personalizedScoreboard = CreatePersonalizedScoreboard(formattedScoreboard, playerScore.Username);
                    await SendToPlayerAsync(roomCode, playerScore.Username, "personal-scoreboard", personalizedScoreboard);
                }
            }

            Console.WriteLine($"[SCORING] Scoreboard sent for room {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SCORING] Error sending scoreboard for room {roomCode}: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Class để parse scoreboard update data
    /// </summary>
    private class ScoreboardUpdateData
    {
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
        public int CorrectAnswers { get; set; }
        public int TotalAnswers { get; set; }
        public double AverageTime { get; set; }
    }

    /// <summary>
    /// Tính toán bảng xếp hạng từ scoring session
    /// </summary>
    private List<ScoreboardEntry> CalculateScoreboard(ScoringSession scoringSession)
    {
        return scoringSession.PlayerScores.Values
            .Select((playerScore, index) => new ScoreboardEntry
            {
                Username = playerScore.Username,
                Score = playerScore.TotalScore,
                Rank = 0, // Sẽ được tính sau khi sort
                CorrectAnswers = playerScore.CorrectAnswers,
                AverageTime = playerScore.AverageTime
            })
            .OrderByDescending(p => p.Score) // Sắp xếp theo điểm số giảm dần
            .ThenBy(p => p.AverageTime) // Nếu bằng điểm thì ai nhanh hơn lên trước
            .Select((entry, index) => {
                entry.Rank = index + 1;
                return entry;
            })
            .ToList();
    }

    /// <summary>
    /// Detect thay đổi vị trí trong bảng xếp hạng
    /// </summary>
    private List<object> DetectPositionChanges(List<ScoreboardEntry> oldScoreboard, List<ScoreboardEntry> newScoreboard)
    {
        var changes = new List<object>();
        
        if (oldScoreboard.Count == 0) return changes;

        foreach (var newEntry in newScoreboard)
        {
            var oldEntry = oldScoreboard.FirstOrDefault(o => o.Username == newEntry.Username);
            if (oldEntry != null && oldEntry.Rank != newEntry.Rank)
            {
                changes.Add(new {
                    username = newEntry.Username,
                    oldRank = oldEntry.Rank,
                    newRank = newEntry.Rank,
                    change = oldEntry.Rank > newEntry.Rank ? "up" : "down"
                });
            }
        }

        return changes;
    }

    /// <summary>
    /// Tính toán kết quả cuối game chi tiết
    /// </summary>
    private (List<object> Rankings, object Statistics, List<object> Achievements, DateTime GameStartTime) CalculateFinalResults(ScoringSession scoringSession)
    {
        var rankings = scoringSession.PlayerScores.Values
            .OrderByDescending(p => p.TotalScore)
            .ThenBy(p => p.AverageTime)
            .Select((player, index) => new {
                rank = index + 1,
                username = player.Username,
                totalScore = player.TotalScore,
                correctAnswers = player.CorrectAnswers,
                totalAnswers = player.TotalAnswers,
                accuracy = player.TotalAnswers > 0 ? (double)player.CorrectAnswers / player.TotalAnswers * 100 : 0,
                averageTime = player.AverageTime,
                maxStreak = player.MaxStreak,
                questionScores = player.QuestionScores
            })
            .ToList<object>();

        var statistics = new {
            totalPlayers = scoringSession.PlayerScores.Count,
            averageScore = scoringSession.PlayerScores.Values.Average(p => p.TotalScore),
            highestScore = scoringSession.PlayerScores.Values.Max(p => p.TotalScore),
            lowestScore = scoringSession.PlayerScores.Values.Min(p => p.TotalScore),
            averageAccuracy = scoringSession.PlayerScores.Values.Average(p => 
                p.TotalAnswers > 0 ? (double)p.CorrectAnswers / p.TotalAnswers * 100 : 0),
            averageTime = scoringSession.PlayerScores.Values.Average(p => p.AverageTime)
        };

        var achievements = CalculateAchievements(scoringSession);
        
        // Estimate game start time (có thể cần lưu chính xác hơn)
        var gameStartTime = scoringSession.PlayerScores.Values
            .Select(p => p.LastAnswerTime)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Min()
            .AddMinutes(-10); // Estimate

        return (rankings, statistics, achievements, gameStartTime);
    }

    /// <summary>
    /// Tính toán achievements cho players
    /// </summary>
    private List<object> CalculateAchievements(ScoringSession scoringSession)
    {
        var achievements = new List<object>();

        // Perfect Score Achievement
        var perfectPlayers = scoringSession.PlayerScores.Values
            .Where(p => p.TotalAnswers > 0 && p.CorrectAnswers == p.TotalAnswers)
            .ToList();
        
        foreach (var player in perfectPlayers)
        {
            achievements.Add(new {
                username = player.Username,
                achievement = "Perfect Score",
                description = "Trả lời đúng tất cả câu hỏi!",
                icon = "🏆"
            });
        }

        // Speed Demon Achievement
        var fastestPlayer = scoringSession.PlayerScores.Values
            .Where(p => p.AverageTime > 0)
            .OrderBy(p => p.AverageTime)
            .FirstOrDefault();
        
        if (fastestPlayer != null)
        {
            achievements.Add(new {
                username = fastestPlayer.Username,
                achievement = "Speed Demon",
                description = $"Trả lời nhanh nhất với thời gian trung bình {fastestPlayer.AverageTime:F1}s",
                icon = "⚡"
            });
        }

        // Streak Master Achievement
        var streakMaster = scoringSession.PlayerScores.Values
            .Where(p => p.MaxStreak >= 5)
            .OrderByDescending(p => p.MaxStreak)
            .FirstOrDefault();
        
        if (streakMaster != null)
        {
            achievements.Add(new {
                username = streakMaster.Username,
                achievement = "Streak Master",
                description = $"Chuỗi trả lời đúng dài nhất: {streakMaster.MaxStreak} câu",
                icon = "🔥"
            });
        }

        return achievements;
    }

    /// <summary>
    /// Tạo kết quả cá nhân cho một player
    /// </summary>
    private object CreatePersonalResult(PlayerScore playerScore, (List<object> Rankings, object Statistics, List<object> Achievements, DateTime GameStartTime) detailedResults)
    {
        var playerRanking = detailedResults.Rankings
            .Cast<dynamic>()
            .FirstOrDefault(r => r.username == playerScore.Username);

        var playerAchievements = detailedResults.Achievements
            .Cast<dynamic>()
            .Where(a => a.username == playerScore.Username)
            .ToList();

        return new {
            personalStats = new {
                rank = playerRanking?.rank ?? 0,
                totalScore = playerScore.TotalScore,
                correctAnswers = playerScore.CorrectAnswers,
                totalAnswers = playerScore.TotalAnswers,
                accuracy = playerScore.TotalAnswers > 0 ? (double)playerScore.CorrectAnswers / playerScore.TotalAnswers * 100 : 0,
                averageTime = playerScore.AverageTime,
                maxStreak = playerScore.MaxStreak,
                questionScores = playerScore.QuestionScores
            },
            achievements = playerAchievements,
            comparison = new {
                betterThanPercent = CalculateBetterThanPercent(playerScore, detailedResults.Rankings),
                scoreVsAverage = playerScore.TotalScore - (detailedResults.Statistics as dynamic)?.averageScore ?? 0
            }
        };
    }

    /// <summary>
    /// Tính phần trăm player tốt hơn bao nhiêu người khác
    /// </summary>
    private double CalculateBetterThanPercent(PlayerScore playerScore, List<object> rankings)
    {
        var totalPlayers = rankings.Count;
        if (totalPlayers <= 1) return 0;

        var playerRank = rankings
            .Cast<dynamic>()
            .FirstOrDefault(r => r.username == playerScore.Username)?.rank ?? totalPlayers;

        var betterThanCount = totalPlayers - playerRank;
        return (double)betterThanCount / (totalPlayers - 1) * 100;
    }

    /// <summary>
    /// Format scoreboard cho client
    /// </summary>
    private object FormatScoreboardForClient(object scoreboard)
    {
        try
        {
            var scoreboardJson = JsonSerializer.Serialize(scoreboard);
            var scoreboardList = JsonSerializer.Deserialize<List<ScoreboardEntry>>(scoreboardJson);
            
            if (scoreboardList != null)
            {
                return scoreboardList.Select(entry => new {
                    username = entry.Username,
                    score = entry.Score,
                    rank = entry.Rank,
                    correctAnswers = entry.CorrectAnswers,
                    averageTime = entry.AverageTime,
                    displayTime = $"{entry.AverageTime:F1}s"
                }).Cast<object>().ToList();
            }
            return new List<object>();
        }
        catch
        {
            return scoreboard;
        }
    }

    /// <summary>
    /// Tạo scoreboard cá nhân hóa cho một player
    /// </summary>
    private object CreatePersonalizedScoreboard(object scoreboard, string username)
    {
        return new {
            scoreboard = scoreboard,
            highlightPlayer = username,
            timestamp = DateTime.UtcNow
        };
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
                        Console.WriteLine($"[SCORING] Failed to send message to {player.Username}: {ex.Message}");
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
                Console.WriteLine($"[SCORING] Failed to send message to {username}: {ex.Message}");
            }
        }
    }

    #endregion
}