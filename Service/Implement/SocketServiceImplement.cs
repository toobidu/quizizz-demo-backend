using ConsoleApp1.Service.Interface;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement;

public class SocketServiceImplement : ISocketService
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ConcurrentDictionary<string, string> _socketToRoom = new();
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    private readonly ConcurrentDictionary<string, RoomGameSession> _roomGameSessions = new();

    private class RoomGameSession
    {
        public int TotalQuestions { get; set; }
        public int GameTimeLimit { get; set; }
        public DateTime GameStartTime { get; set; }
        public bool IsGameActive { get; set; }
        public bool IsGameEnded { get; set; }
        public Dictionary<string, PlayerGameResult> PlayerResults { get; set; } = new();
        public List<QuestionData> Questions { get; set; } = new();
        public Timer? GameTimer { get; set; }
    }

    private class PlayerGameResult
    {
        public string Username { get; set; } = string.Empty;
        public List<PlayerAnswer> Answers { get; set; } = new();
        public DateTime? CompletionTime { get; set; }
        public int Score { get; set; }
        public bool HasFinishedAllQuestions => Answers.Count >= TotalQuestions;
        public int TotalQuestions { get; set; }
    }

    public async Task StartAsync(int port)
    {
        Console.WriteLine($"[SOCKET] WebSocket service initialized on port {port}");
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        foreach (var connection in _connections.Values)
        {
            if (connection.State == WebSocketState.Open)
            {
                await connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutdown", CancellationToken.None);
            }
        }
        _connections.Clear();
        Console.WriteLine("[SOCKET] Service stopped");
    }

    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId)
    {
        _socketToRoom[socketId] = roomCode;
        
        if (!_gameRooms.ContainsKey(roomCode))
        {
            _gameRooms[roomCode] = new GameRoom { RoomCode = roomCode };
        }
        
        var gameRoom = _gameRooms[roomCode];
        var player = new GamePlayer
        {
            Username = username,
            UserId = userId,
            SocketId = socketId,
            IsHost = gameRoom.Players.Count == 0,
            JoinTime = DateTime.UtcNow
        };
        
        gameRoom.Players.Add(player);
        await UpdateRoomPlayersAsync(roomCode);
        
        Console.WriteLine($"[SOCKET] {username} joined room {roomCode} as {(player.IsHost ? "host" : "player")}");
    }

    public async Task LeaveRoomAsync(string socketId, string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.SocketId == socketId);
        
        if (player != null)
        {
            gameRoom.Players.Remove(player);
            
            // Nếu là host rời phòng
            if (player.IsHost && gameRoom.Players.Count > 0)
            {
                // Chuyển host cho người join sớm nhất tiếp theo
                var nextHost = gameRoom.Players.OrderBy(p => p.JoinTime ?? DateTime.MaxValue).First();
                nextHost.IsHost = true;
                
                await BroadcastToRoomAsync(roomCode, "host-changed", new {
                    newHost = nextHost.Username,
                    message = $"{nextHost.Username} đã trở thành host mới"
                });
            }
            
            if (gameRoom.Players.Count == 0)
            {
                _gameRooms.TryRemove(roomCode, out _);
                await CleanupGameSessionAsync(roomCode);
                Console.WriteLine($"[SOCKET] Room {roomCode} deleted - no players left");
            }
            else
            {
                await UpdateRoomPlayersAsync(roomCode);
                await BroadcastToRoomAsync(roomCode, "player-left", new {
                    username = player.Username,
                    message = $"{player.Username} đã rời phòng",
                    remainingPlayers = gameRoom.Players.Count
                });
            }
        }
        
        _socketToRoom.TryRemove(socketId, out _);
        Console.WriteLine($"[SOCKET] {player?.Username ?? socketId} left room {roomCode}");
    }

    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var questionList = JsonSerializer.Deserialize<List<QuestionData>>(questions.ToString() ?? "") ?? new();
        
        var gameSession = new RoomGameSession
        {
            TotalQuestions = questionList.Count,
            GameTimeLimit = gameTimeLimit,
            GameStartTime = DateTime.UtcNow,
            IsGameActive = true,
            Questions = questionList
        };
        
        _roomGameSessions[roomCode] = gameSession;
        
        var gameRoom = _gameRooms[roomCode];
        foreach (var player in gameRoom.Players)
        {
            gameSession.PlayerResults[player.Username] = new PlayerGameResult 
            { 
                Username = player.Username,
                TotalQuestions = questionList.Count
            };
        }
        
        StartGlobalGameTimer(roomCode, gameTimeLimit);
        
        await BroadcastToRoomAsync(roomCode, "game-started", new { 
            totalQuestions = questionList.Count, 
            gameTimeLimit,
            startTime = gameSession.GameStartTime
        });
        
        Console.WriteLine($"[SOCKET] Game started in room {roomCode} with {questionList.Count} questions, {gameTimeLimit}s total time");
    }

    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        if (!gameSession.IsGameActive || gameSession.IsGameEnded) return;
        
        var playerResult = gameSession.PlayerResults.GetValueOrDefault(username);
        if (playerResult == null) return;
        
        int nextQuestionIndex = playerResult.Answers.Count;
        
        if (nextQuestionIndex >= gameSession.TotalQuestions)
        {
            var player = _gameRooms[roomCode].Players.FirstOrDefault(p => p.Username == username);
            if (player != null)
            {
                await SendToPlayerAsync(player.SocketId, "all-questions-completed", new {
                    message = "Bạn đã hoàn thành tất cả câu hỏi. Vui lòng chờ các người chơi khác."
                });
            }
            return;
        }
        
        var question = gameSession.Questions[nextQuestionIndex];
        var targetPlayer = _gameRooms[roomCode].Players.FirstOrDefault(p => p.Username == username);
        
        if (targetPlayer != null)
        {
            await SendToPlayerAsync(targetPlayer.SocketId, "next-question", new {
                question,
                questionIndex = nextQuestionIndex + 1,
                totalQuestions = gameSession.TotalQuestions,
                timeRemaining = GetRemainingGameTime(roomCode)
            });
        }
        
        Console.WriteLine($"[SOCKET] Question {nextQuestionIndex + 1}/{gameSession.TotalQuestions} sent to {username} in room {roomCode}");
    }

    public async Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        if (!gameSession.IsGameActive || gameSession.IsGameEnded) return;
        
        var playerResult = gameSession.PlayerResults.GetValueOrDefault(username);
        if (playerResult == null) return;
        
        int questionIndex = playerResult.Answers.Count;
        if (questionIndex >= gameSession.TotalQuestions) return;
        
        playerResult.Answers.Add(new PlayerAnswer
        {
            Username = username,
            Answer = answer,
            Timestamp = timestamp,
            QuestionIndex = questionIndex
        });
        
        if (playerResult.HasFinishedAllQuestions && !playerResult.CompletionTime.HasValue)
        {
            playerResult.CompletionTime = DateTime.UtcNow;
            
            var player = _gameRooms[roomCode].Players.FirstOrDefault(p => p.Username == username);
            if (player != null)
            {
                await SendToPlayerAsync(player.SocketId, "all-questions-completed", new {
                    message = "Bạn đã hoàn thành tất cả câu hỏi. Vui lòng chờ thời gian kết thúc.",
                    completionTime = playerResult.CompletionTime
                });
            }
            
            Console.WriteLine($"[SOCKET] {username} completed all questions in room {roomCode} at {playerResult.CompletionTime}");
        }
        
        await BroadcastToRoomAsync(roomCode, "answer-received", new { 
            username, 
            questionIndex = questionIndex + 1,
            totalAnswered = playerResult.Answers.Count,
            totalQuestions = gameSession.TotalQuestions
        });
        
        Console.WriteLine($"[SOCKET] Answer from {username} in room {roomCode} for question {questionIndex + 1}");
    }

    public async Task SendScoreboardAsync(string roomCode, object scoreboard)
    {
        await BroadcastToRoomAsync(roomCode, "scoreboard-update", scoreboard);
        Console.WriteLine($"[SOCKET] Scoreboard sent to room {roomCode}");
    }

    public async Task EndGameAsync(string roomCode, object finalResults)
    {
        await BroadcastToRoomAsync(roomCode, "game-ended", finalResults);
        Console.WriteLine($"[SOCKET] Game ended in room {roomCode}");
    }

    public async Task UpdateRoomPlayersAsync(string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var playerList = gameRoom.Players.Select(p => new 
        { 
            username = p.Username, 
            isHost = p.IsHost, 
            status = p.Status,
            score = p.Score 
        }).ToList();
        
        await BroadcastToRoomAsync(roomCode, "room-players-updated", new 
        { 
            players = playerList, 
            playerCount = gameRoom.Players.Count,
            gameState = gameRoom.GameState
        });
    }

    public async Task UpdatePlayerStatusAsync(string roomCode, string username, string status)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.Username == username);
        
        if (player != null)
        {
            player.Status = status;
            await UpdateRoomPlayersAsync(roomCode);
        }
    }

    public async Task StartGameAsync(string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        gameRoom.GameState = "starting";
        
        await BroadcastToRoomAsync(roomCode, "game-starting", new { countdown = 3 });
        
        for (int i = 3; i > 0; i--)
        {
            await SendCountdownAsync(roomCode, i);
            await Task.Delay(1000);
        }
        
        gameRoom.GameState = "active";
        await BroadcastToRoomAsync(roomCode, "game-started", new { });
        Console.WriteLine($"[SOCKET] Game started in room {roomCode}");
    }

    public async Task SendCountdownAsync(string roomCode, int countdown)
    {
        await BroadcastToRoomAsync(roomCode, "countdown", new { count = countdown });
    }

    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        gameRoom.CurrentQuestionIndex = questionIndex;
        gameRoom.TotalQuestions = totalQuestions;
        gameRoom.QuestionStartTime = DateTime.UtcNow;
        gameRoom.GameState = "active";
        
        foreach (var player in gameRoom.Players)
        {
            player.Status = "answering";
        }
        
        await BroadcastToRoomAsync(roomCode, "new-question", new 
        { 
            question,
            questionIndex = questionIndex + 1,
            totalQuestions,
            gameTimeRemaining = GetRemainingGameTime(roomCode)
        });
        
        Console.WriteLine($"[SOCKET] Question {questionIndex + 1}/{totalQuestions} sent to room {roomCode}");
    }

    public async Task SendGameTimerUpdateAsync(string roomCode)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var remainingTime = GetRemainingGameTime(roomCode);
        await BroadcastToRoomAsync(roomCode, "game-timer-update", new { timeRemaining = remainingTime });
    }

    public async Task GetPlayerProgressAsync(string roomCode, string username)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        var playerResult = gameSession.PlayerResults.GetValueOrDefault(username);
        
        if (playerResult != null)
        {
            var player = _gameRooms[roomCode].Players.FirstOrDefault(p => p.Username == username);
            if (player != null)
            {
                await SendToPlayerAsync(player.SocketId, "player-progress", new {
                    answeredQuestions = playerResult.Answers.Count,
                    totalQuestions = gameSession.TotalQuestions,
                    isCompleted = playerResult.HasFinishedAllQuestions,
                    completionTime = playerResult.CompletionTime,
                    gameTimeRemaining = GetRemainingGameTime(roomCode)
                });
            }
        }
    }

    public async Task BroadcastPlayerProgressAsync(string roomCode)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        var progressData = new List<object>();
        
        foreach (var playerResult in gameSession.PlayerResults.Values)
        {
            progressData.Add(new {
                username = playerResult.Username,
                answeredQuestions = playerResult.Answers.Count,
                totalQuestions = gameSession.TotalQuestions,
                isCompleted = playerResult.HasFinishedAllQuestions
            });
        }
        
        await BroadcastToRoomAsync(roomCode, "players-progress-update", new {
            playersProgress = progressData,
            gameTimeRemaining = GetRemainingGameTime(roomCode)
        });
    }

    public Task CleanupGameSessionAsync(string roomCode)
    {
        if (_roomGameSessions.TryRemove(roomCode, out var gameSession))
        {
            gameSession.GameTimer?.Dispose();
            Console.WriteLine($"[SOCKET] Game session cleaned up for room {roomCode}");
        }
        return Task.CompletedTask;
    }

    public async Task UpdateScoreboardAsync(string roomCode, object scoreboard)
    {
        await BroadcastToRoomAsync(roomCode, "scoreboard-updated", scoreboard);
        Console.WriteLine($"[SOCKET] Scoreboard updated in room {roomCode}");
    }

    public async Task SendFinalResultsAsync(string roomCode, object finalResults)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        gameRoom.GameState = "finished";
        
        await BroadcastToRoomAsync(roomCode, "game-finished", finalResults);
        Console.WriteLine($"[SOCKET] Final results sent to room {roomCode}");
    }

    public async Task UpdateGameStateAsync(string roomCode, string gameState)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        gameRoom.GameState = gameState;
        
        await BroadcastToRoomAsync(roomCode, "game-state-changed", new { gameState });
    }

    public async Task NotifyHostOnlyAsync(string roomCode, string message)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var host = gameRoom.Players.FirstOrDefault(p => p.IsHost);
        
        if (host != null)
        {
            await SendToPlayerAsync(host.SocketId, "host-notification", new { message });
        }
    }

    public async Task RequestNextQuestionAsync(string roomCode)
    {
        await BroadcastToRoomAsync(roomCode, "next-question-requested", new { });
        Console.WriteLine($"[SOCKET] Next question requested for room {roomCode}");
    }

    private void StartGlobalGameTimer(string roomCode, int gameTimeLimit)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        
        gameSession.GameTimer = new Timer(async _ =>
        {
            await EndGameAndShowResults(roomCode);
        }, null, TimeSpan.FromSeconds(gameTimeLimit), Timeout.InfiniteTimeSpan);
        
        Console.WriteLine($"[SOCKET] Global timer started for room {roomCode} - {gameTimeLimit} seconds");
    }

    private async Task EndGameAndShowResults(string roomCode)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return;
        
        var gameSession = _roomGameSessions[roomCode];
        if (gameSession.IsGameEnded) return;
        
        gameSession.IsGameEnded = true;
        gameSession.IsGameActive = false;
        
        gameSession.GameTimer?.Dispose();
        
        var finalResults = CalculateFinalResults(roomCode);
        
        await BroadcastToRoomAsync(roomCode, "game-ended", new {
            finalResults,
            message = "Thời gian đã hết! Hiển thị kết quả cuối cùng."
        });
        
        Console.WriteLine($"[SOCKET] Game ended in room {roomCode} - showing final results");
    }

    private object CalculateFinalResults(string roomCode)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return new { };
        
        var gameSession = _roomGameSessions[roomCode];
        var results = new List<object>();
        
        foreach (var playerResult in gameSession.PlayerResults.Values)
        {
            results.Add(new {
                username = playerResult.Username,
                totalAnswers = playerResult.Answers.Count,
                totalQuestions = gameSession.TotalQuestions,
                completionTime = playerResult.CompletionTime,
                score = playerResult.Score,
                isCompleted = playerResult.HasFinishedAllQuestions
            });
        }
        
        results = results.OrderByDescending(r => ((dynamic)r).totalAnswers)
                        .ThenBy(r => ((dynamic)r).completionTime ?? DateTime.MaxValue)
                        .ToList();
        
        return new {
            rankings = results,
            gameEndTime = DateTime.UtcNow,
            totalGameTime = gameSession.GameTimeLimit
        };
    }

    private int GetRemainingGameTime(string roomCode)
    {
        if (!_roomGameSessions.ContainsKey(roomCode)) return 0;
        
        var gameSession = _roomGameSessions[roomCode];
        var elapsed = (DateTime.UtcNow - gameSession.GameStartTime).TotalSeconds;
        var remaining = Math.Max(0, gameSession.GameTimeLimit - (int)elapsed);
        
        return remaining;
    }

    private async Task BroadcastToRoomAsync(string roomCode, string eventName, object data)
    {
        if (!_gameRooms.ContainsKey(roomCode)) return;
        
        var gameRoom = _gameRooms[roomCode];
        var message = JsonSerializer.Serialize(new { eventName, data });
        var buffer = Encoding.UTF8.GetBytes(message);
        
        foreach (var player in gameRoom.Players)
        {
            if (_connections.TryGetValue(player.SocketId, out var socket) && socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }

    private async Task SendToPlayerAsync(string socketId, string eventName, object data)
    {
        if (_connections.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
        {
            var message = JsonSerializer.Serialize(new { eventName, data });
            var buffer = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}