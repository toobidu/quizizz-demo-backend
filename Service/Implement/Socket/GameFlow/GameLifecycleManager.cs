using ConsoleApp1.Model.DTO.Game;
using ConsoleApp1.Service.Helper;
using ConsoleApp1.Data;
using ConsoleApp1.Config;
using System.Collections.Concurrent;
using System.Text.Json;
namespace ConsoleApp1.Service.Implement.Socket.GameFlow;
/// <summary>
/// Quản lý vòng đời game - Chịu trách nhiệm:
/// 1. Bắt đầu và kết thúc game
/// 2. Quản lý trạng thái game
/// 3. Xử lý countdown
/// 4. Dọn dẹp resources
/// </summary>
public class GameLifecycleManager
{
    private readonly GameSessionManager _sessionManager;
    private readonly GameTimerManager _timerManager;
    private readonly GameEventBroadcaster _eventBroadcaster;
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;

    public GameLifecycleManager(
        GameSessionManager sessionManager,
        GameTimerManager timerManager,
        GameEventBroadcaster eventBroadcaster,
        ConcurrentDictionary<string, GameRoom> gameRooms)
    {
        _sessionManager = sessionManager;
        _timerManager = timerManager;
        _eventBroadcaster = eventBroadcaster;
        _gameRooms = gameRooms;
    }
    /// <summary>
    /// Bắt đầu game với câu hỏi thực sự theo topic đã chọn
    /// </summary>
    public async Task StartSimpleGameAsync(string roomCode)
    {
        try
        {
            // 1. Kiểm tra và lấy thông tin phòng từ memory hoặc database (robust approach)
            GameRoom? gameRoom = null;
            
            if (_gameRooms.ContainsKey(roomCode))
            {
                gameRoom = _gameRooms[roomCode];
                Console.WriteLine($"✅ [GameLifecycleManager] Found room {roomCode} in memory with {gameRoom.Players.Count} players");
            }
            else
            {
                Console.WriteLine($"⚠️ [GameLifecycleManager] Room {roomCode} not found in memory, loading from database...");
                
                // Lấy dependencies từ DatabaseHelper
                var config = ConfigLoader.Load();
                var connectionString = config.ConnectionStrings["DefaultConnection"];
                var dbHelper = new DatabaseHelper(connectionString);
                var roomRepoForFallback = new ConsoleApp1.Repository.Implement.RoomRepositoryImplement(dbHelper);
                var roomPlayerRepo = new ConsoleApp1.Repository.Implement.RoomPlayerRepositoryImplement(dbHelper);
                var userRepo = new ConsoleApp1.Repository.Implement.UserRepositoryImplement(dbHelper);
                
                // Lấy thông tin phòng từ database
                var roomFromDb = await roomRepoForFallback.GetRoomByCodeAsync(roomCode);
                if (roomFromDb == null)
                {
                    Console.WriteLine($"❌ [GameLifecycleManager] Room {roomCode} not found in database either");
                    return;
                }
                
                // Lấy danh sách players từ database  
                var roomPlayers = await roomPlayerRepo.GetByRoomIdAsync(roomFromDb.Id);
                var players = new List<GamePlayer>();
                
                foreach (var rp in roomPlayers)
                {
                    var user = await userRepo.GetUserByIdAsync(rp.UserId);
                    players.Add(new GamePlayer
                    {
                        Username = user?.Username ?? $"User_{rp.UserId}",
                        UserId = rp.UserId,
                        SocketId = rp.SocketId ?? "",
                        Score = rp.Score,
                        Status = rp.Status ?? "waiting",
                        IsHost = rp.UserId == roomFromDb.OwnerId,
                        JoinTime = rp.CreatedAt
                    });
                }
                
                // Tạo GameRoom tạm thời từ dữ liệu database
                gameRoom = new GameRoom
                {
                    RoomCode = roomCode,
                    Players = players,
                    GameState = GameFlowConstants.GameStates.Waiting,
                    CurrentQuestionIndex = 0,
                    TotalQuestions = 0,
                    CreatedAt = roomFromDb.CreatedAt
                };
                
                // Thêm vào memory để sử dụng tiếp (important!)
                _gameRooms.TryAdd(roomCode, gameRoom);
                Console.WriteLine($"✅ [GameLifecycleManager] Created temporary GameRoom for {roomCode} with {gameRoom.Players.Count} players");
            }
            
            // 2. Kiểm tra có đủ người chơi không (tối thiểu 1 người)
            if (gameRoom.Players.Count == 0)
            {
                Console.WriteLine($"❌ [GameLifecycleManager] No players in room {roomCode}");
                return;
            }

            // 3. Lấy dependencies từ DatabaseHelper (tái sử dụng nếu đã tạo ở trên)
            var config2 = ConfigLoader.Load();
            var connectionString2 = config2.ConnectionStrings["DefaultConnection"];
            var dbHelper2 = new DatabaseHelper(connectionString2);
            var questionRepo = new ConsoleApp1.Repository.Implement.QuestionRepositoryImplement(dbHelper2);
            var roomSettingsRepo = new ConsoleApp1.Repository.Implement.RoomSettingsRepositoryImplement(dbHelper2);
            var roomRepo = new ConsoleApp1.Repository.Implement.RoomRepositoryImplement(dbHelper2);
            var gameSessionRepo = new ConsoleApp1.Repository.Implement.GameSessionRepositoryImplement(dbHelper2);
            var gameQuestionRepo = new ConsoleApp1.Repository.Implement.GameQuestionRepositoryImplement(dbHelper2);
            var gameSessionService = new ConsoleApp1.Service.Implement.GameSessionServiceImplement(gameSessionRepo, gameQuestionRepo, questionRepo);

            // 4. Sử dụng helper để lấy câu hỏi theo topic
            var questionDataList = await GameQuestionHelper.GetQuestionsForRoomAsync(
                roomCode, 
                questionRepo, 
                roomSettingsRepo, 
                roomRepo);

            if (questionDataList.Count == 0)
            {
                Console.WriteLine($"❌ [GameLifecycleManager] No questions found for room {roomCode}");
                // Fallback về logic cũ
                var gameSession = _sessionManager.CreateSimpleGameSession(roomCode);
                gameRoom.GameState = GameFlowConstants.GameStates.Playing;
                var duLieuSuKien = new GameStartEventData
                {
                    Message = GameFlowConstants.Messages.GameStarted,
                    StartTime = gameSession.GameStartTime,
                    RoomCode = roomCode,
                    TotalQuestions = 0
                };
                await _eventBroadcaster.BroadcastGameStartedAsync(roomCode, duLieuSuKien);
                Console.WriteLine($"⚠️ [GameLifecycleManager] Fallback: game-started broadcasted to room {roomCode} with 0 questions");
                return;
            }

            // 5. Lấy thông tin room để tạo GameSession trong database
            var room = await roomRepo.GetRoomByCodeAsync(roomCode);
            if (room != null)
            {
                // Tạo GameSession trong database
                var gameSession = new ConsoleApp1.Model.Entity.Rooms.GameSession
                {
                    RoomId = room.Id,
                    GameState = "playing",
                    CurrentQuestionIndex = 0,
                    TimeLimit = 30 * questionDataList.Count,
                    StartTime = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var gameSessionId = await gameSessionService.CreateAsync(gameSession);
                var questionIds = questionDataList.Select(q => q.QuestionId).ToList();
                await gameSessionService.AddQuestionsToGameSessionAsync(gameSessionId, questionIds, 30);
                Console.WriteLine($"💾 [GameLifecycleManager] Created GameSession {gameSessionId} with {questionIds.Count} questions");
            }

            // 6. Tạo phiên game trong memory với câu hỏi đã lấy
            var inMemoryGameSession = _sessionManager.CreateGameSession(roomCode, questionDataList, 30 * questionDataList.Count);

            // 7. Khởi tạo tiến độ cho tất cả người chơi
            _sessionManager.InitializePlayerProgress(roomCode, gameRoom.Players);

            // 8. Thay đổi trạng thái phòng thành "đang chơi"
            gameRoom.GameState = GameFlowConstants.GameStates.Playing;

            // 9. Broadcast thông báo game bắt đầu với thông tin đầy đủ
            var duLieuSuKienDayDu = new GameStartEventData
            {
                Message = GameFlowConstants.Messages.GameStarted,
                StartTime = inMemoryGameSession.GameStartTime,
                RoomCode = roomCode,
                TotalQuestions = questionDataList.Count,
                TimeLimit = 30
            };
            await _eventBroadcaster.BroadcastGameStartedAsync(roomCode, duLieuSuKienDayDu);
            
            Console.WriteLine($"✅ [GameLifecycleManager] game-started broadcasted to room {roomCode} with {gameRoom.Players.Count} players, {questionDataList.Count} questions");

            // 10. Gửi countdown 3-2-1 trước khi bắt đầu
            await SendCountdownAsync(roomCode, GameFlowConstants.Defaults.CountdownSeconds);

            // 11. Bắt đầu timer countdown
            _timerManager.CreateCountdownTimer(roomCode, GameFlowConstants.Defaults.CountdownSeconds, 
                async (count) =>
                {
                    if (count > 0)
                    {
                        var countdownData = new CountdownEventData { Count = count, Message = count.ToString() };
                        await _eventBroadcaster.BroadcastCountdownAsync(roomCode, countdownData);
                    }
                },
                async () =>
                {
                    // Khi countdown = 0 thì bắt đầu game thực sự
                    var countdownData = new CountdownEventData { Count = 0, Message = GameFlowConstants.Messages.CountdownStart };
                    await _eventBroadcaster.BroadcastCountdownAsync(roomCode, countdownData);

                    // 12. Gửi câu hỏi đầu tiên nếu có
                    if (inMemoryGameSession.Questions.Count > 0)
                    {
                        var firstQuestion = inMemoryGameSession.Questions[0];
                        var questionData = new QuestionEventData
                        {
                            Question = firstQuestion,
                            QuestionIndex = 0,
                            TotalQuestions = inMemoryGameSession.Questions.Count,
                            TimeRemaining = GetRemainingGameTime(inMemoryGameSession),
                            GameState = GameFlowConstants.GameStates.QuestionActive
                        };
                        await _eventBroadcaster.BroadcastNewQuestionAsync(roomCode, questionData);
                        Console.WriteLine($"📤 [GameLifecycleManager] Sent first question to room {roomCode}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ [GameLifecycleManager] No questions to send after countdown for room {roomCode}.");
                    }
                });

            // 13. Cập nhật trạng thái phòng
            await UpdateGameStateAsync(roomCode, GameFlowConstants.GameStates.Playing);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameLifecycleManager] Error in StartSimpleGameAsync: {ex.Message}");
            Console.WriteLine($"❌ [GameLifecycleManager] Stack Trace: {ex.StackTrace}");
        }
    }
    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int timeLimit)
    {
        try
        {
            // Kiểm tra thời gian giới hạn hợp lệ
            if (!IsValidTimeLimit(timeLimit))
            {
                return;
            }
            // Parse danh sách câu hỏi từ JSON
            var questionsJson = JsonSerializer.Serialize(questions);
            var questionsList = JsonSerializer.Deserialize<List<QuestionData>>(questionsJson) ?? new List<QuestionData>();
            if (questionsList.Count == 0)
            {
                return;
            }
            // Tạo game session với câu hỏi
            var gameSession = _sessionManager.CreateGameSession(roomCode, questionsList, timeLimit);
            // Khởi tạo tiến độ cho tất cả người chơi
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                _sessionManager.InitializePlayerProgress(roomCode, gameRoom.Players);
            }
            // Gửi countdown 3-2-1 trước khi bắt đầu
            await SendCountdownAsync(roomCode, GameFlowConstants.Defaults.CountdownSeconds);
            // Khởi tạo timer cho game (tự động kết thúc khi hết thời gian)
            _timerManager.CreateGameTimer(roomCode, timeLimit, async () =>
            {
                await EndGameDueToTimeoutAsync(roomCode);
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    public async Task SendCountdownAsync(string roomCode, int countdown)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession == null)
            {
                return;
            }
            // Tạo countdown timer
            _timerManager.CreateCountdownTimer(roomCode, countdown, 
                async (count) =>
                {
                    // Broadcast số đếm ngược
                    var eventData = new CountdownEventData
                    {
                        Count = count,
                        Message = count.ToString()
                    };
                    await _eventBroadcaster.BroadcastCountdownAsync(roomCode, eventData);
                },
                async () =>
                {
                    // Khi countdown = 0 thì bắt đầu game thực sự
                    var startData = new CountdownEventData
                    {
                        Count = 0,
                        Message = GameFlowConstants.Messages.CountdownStart
                    };
                    await _eventBroadcaster.BroadcastCountdownAsync(roomCode, startData);
                    // Gửi câu hỏi đầu tiên nếu có
                    if (gameSession.Questions.Count > 0)
                    {
                        var questionData = new QuestionEventData
                        {
                            Question = gameSession.Questions[0],
                            QuestionIndex = 0,
                            TotalQuestions = gameSession.Questions.Count,
                            TimeRemaining = GetRemainingGameTime(gameSession),
                            GameState = GameFlowConstants.GameStates.QuestionActive
                        };
                        await _eventBroadcaster.BroadcastNewQuestionAsync(roomCode, questionData);
                    }
                });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Cập nhật trạng thái game
    /// </summary>
    public async Task UpdateGameStateAsync(string roomCode, string gameState)
    {
        try
        {
            // Cập nhật trạng thái trong game session
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession != null)
            {
                gameSession.IsGameActive = gameState == GameFlowConstants.GameStates.Playing;
                if (gameState == GameFlowConstants.GameStates.Ended)
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
            await _eventBroadcaster.BroadcastGameStateChangedAsync(roomCode, gameState);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Kết thúc game thủ công (do host)
    /// </summary>
    public async Task EndGameManuallyAsync(string roomCode, string reason = "Host kết thúc game")
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession == null)
            {
                return;
            }
            // Kết thúc game session
            _sessionManager.EndGameSession(roomCode);
            // Tạo kết quả cuối cùng
            var finalResults = gameSession.PlayerProgress.Values.Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count
            }).OrderByDescending(p => p.score).ToList<object>();
            // Tạo dữ liệu sự kiện
            var eventData = new GameEndEventData
            {
                Reason = GameFlowConstants.EndReasons.HostEnded,
                Message = reason,
                FinalResults = finalResults
            };
            await _eventBroadcaster.BroadcastGameEndedAsync(roomCode, eventData);
            // Dọn dẹp sau delay
            _ = Task.Delay(TimeSpan.FromSeconds(GameFlowConstants.Defaults.CleanupDelaySeconds))
                .ContinueWith(async _ => await CleanupGameSessionAsync(roomCode));
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Dọn dẹp game session khi kết thúc
    /// </summary>
    public async Task CleanupGameSessionAsync(string roomCode)
    {
        try
        {
            // Dừng tất cả timer
            _timerManager.DisposeAllTimersForRoom(roomCode);
            // Cleanup game session
            _sessionManager.CleanupGameSession(roomCode);
            // Reset trạng thái phòng về "chờ"
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                gameRoom.GameState = GameFlowConstants.GameStates.Waiting;
                gameRoom.CurrentQuestionIndex = 0;
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Tạm dừng game
    /// </summary>
    public async Task PauseGameAsync(string roomCode)
    {
        try
        {
            await UpdateGameStateAsync(roomCode, "paused");
            // Dừng tất cả timer tạm thời
            _timerManager.DisposeAllTimersForRoom(roomCode);
            // Broadcast thông báo tạm dừng
            await _eventBroadcaster.BroadcastGameStateChangedAsync(roomCode, "paused");
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Tiếp tục game sau khi tạm dừng
    /// </summary>
    public async Task ResumeGameAsync(string roomCode)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession == null) return;
            await UpdateGameStateAsync(roomCode, GameFlowConstants.GameStates.Playing);
            // Tính thời gian còn lại và tạo lại timer
            var remainingTime = GetRemainingGameTime(gameSession);
            if (remainingTime > 0)
            {
                _timerManager.CreateGameTimer(roomCode, remainingTime, async () =>
                {
                    await EndGameDueToTimeoutAsync(roomCode);
                });
            }
            // Broadcast thông báo tiếp tục
            await _eventBroadcaster.BroadcastGameStateChangedAsync(roomCode, GameFlowConstants.GameStates.Playing);
        }
        catch (Exception ex)
        {
        }
    }
    #region Private Helper Methods
    /// <summary>
    /// Kiểm tra thời gian giới hạn có hợp lệ không
    /// </summary>
    private bool IsValidTimeLimit(int thoiGianGioiHan)
    {
        return thoiGianGioiHan >= GameFlowConstants.Limits.MinGameTimeLimit && 
               thoiGianGioiHan <= GameFlowConstants.Limits.MaxGameTimeLimit;
    }
    /// <summary>
    /// Tính thời gian còn lại của game (giây)
    /// </summary>
    private int GetRemainingGameTime(GameSession gameSession)
    {
        if (!gameSession.IsGameActive) return 0;
        var thoiGianDaTroi = (DateTime.UtcNow - gameSession.GameStartTime).TotalSeconds;
        var thoiGianConLai = gameSession.GameTimeLimit - thoiGianDaTroi;
        return Math.Max(0, (int)thoiGianConLai);
    }
    /// <summary>
    /// Kết thúc game do hết thời gian
    /// </summary>
    private async Task EndGameDueToTimeoutAsync(string roomCode)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession == null) return;
            // Kết thúc game session
            _sessionManager.EndGameSession(roomCode);
            // Tạo kết quả cuối cùng
            var finalResults = gameSession.PlayerProgress.Values.Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count
            }).OrderByDescending(p => p.score).ToList<object>();
            // Tạo dữ liệu sự kiện
            var eventData = new GameEndEventData
            {
                Reason = GameFlowConstants.EndReasons.Timeout,
                Message = GameFlowConstants.Messages.GameEndedTimeout,
                FinalResults = finalResults
            };
            await _eventBroadcaster.BroadcastGameEndedAsync(roomCode, eventData);
            // Dọn dẹp sau delay
            _ = Task.Delay(TimeSpan.FromSeconds(GameFlowConstants.Defaults.CleanupDelaySeconds))
                .ContinueWith(async _ => await CleanupGameSessionAsync(roomCode));
        }
        catch (Exception ex)
        {
        }
    }
    #endregion
}
