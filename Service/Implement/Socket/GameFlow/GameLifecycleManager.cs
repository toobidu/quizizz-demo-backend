using ConsoleApp1.Model.DTO.Game;
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
    /// Bắt đầu game đơn giản (không có câu hỏi)
    /// </summary>
    public async Task StartSimpleGameAsync(string maPhong)
    {
        try
        {
            // Kiểm tra phòng có tồn tại không
            if (!_gameRooms.ContainsKey(maPhong))
            {
                return;
            }
            var gameRoom = _gameRooms[maPhong];
            // Kiểm tra có đủ người chơi không (tối thiểu 1 người)
            if (gameRoom.Players.Count == 0)
            {
                return;
            }
            // Tạo game session đơn giản
            var gameSession = _sessionManager.CreateSimpleGameSession(maPhong);
            // Thay đổi trạng thái phòng thành "đang chơi"
            gameRoom.GameState = GameFlowConstants.GameStates.Playing;
            // Broadcast thông báo game bắt đầu
            var duLieuSuKien = new GameStartEventData
            {
                Message = GameFlowConstants.Messages.GameStarted,
                StartTime = gameSession.GameStartTime,
                RoomCode = maPhong
            };
            await _eventBroadcaster.BroadcastGameStartedAsync(maPhong, duLieuSuKien);
            
            Console.WriteLine($"✅ [Backend GameLifecycle] game-started broadcasted to room {maPhong} with {gameRoom.Players.Count} players");
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    public async Task StartGameWithQuestionsAsync(string maPhong, object cauHoi, int thoiGianGioiHan)
    {
        try
        {
            // Kiểm tra thời gian giới hạn hợp lệ
            if (!IsValidTimeLimit(thoiGianGioiHan))
            {
                return;
            }
            // Parse danh sách câu hỏi từ JSON
            var cauHoiJson = JsonSerializer.Serialize(cauHoi);
            var danhSachCauHoi = JsonSerializer.Deserialize<List<QuestionData>>(cauHoiJson) ?? new List<QuestionData>();
            if (danhSachCauHoi.Count == 0)
            {
                return;
            }
            // Tạo game session với câu hỏi
            var gameSession = _sessionManager.CreateGameSession(maPhong, danhSachCauHoi, thoiGianGioiHan);
            // Khởi tạo tiến độ cho tất cả người chơi
            if (_gameRooms.TryGetValue(maPhong, out var gameRoom))
            {
                _sessionManager.InitializePlayerProgress(maPhong, gameRoom.Players);
            }
            // Gửi countdown 3-2-1 trước khi bắt đầu
            await SendCountdownAsync(maPhong, GameFlowConstants.Defaults.CountdownSeconds);
            // Khởi tạo timer cho game (tự động kết thúc khi hết thời gian)
            _timerManager.CreateGameTimer(maPhong, thoiGianGioiHan, async () =>
            {
                await EndGameDueToTimeoutAsync(maPhong);
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    public async Task SendCountdownAsync(string maPhong, int demNguoc)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return;
            }
            // Tạo countdown timer
            _timerManager.CreateCountdownTimer(maPhong, demNguoc, 
                async (count) =>
                {
                    // Broadcast số đếm ngược
                    var duLieuSuKien = new CountdownEventData
                    {
                        Count = count,
                        Message = count.ToString()
                    };
                    await _eventBroadcaster.BroadcastCountdownAsync(maPhong, duLieuSuKien);
                },
                async () =>
                {
                    // Khi countdown = 0 thì bắt đầu game thực sự
                    var duLieuBatDau = new CountdownEventData
                    {
                        Count = 0,
                        Message = GameFlowConstants.Messages.CountdownStart
                    };
                    await _eventBroadcaster.BroadcastCountdownAsync(maPhong, duLieuBatDau);
                    // Gửi câu hỏi đầu tiên nếu có
                    if (gameSession.Questions.Count > 0)
                    {
                        var duLieuCauHoi = new QuestionEventData
                        {
                            Question = gameSession.Questions[0],
                            QuestionIndex = 0,
                            TotalQuestions = gameSession.Questions.Count,
                            TimeRemaining = GetRemainingGameTime(gameSession),
                            GameState = GameFlowConstants.GameStates.QuestionActive
                        };
                        await _eventBroadcaster.BroadcastNewQuestionAsync(maPhong, duLieuCauHoi);
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
    public async Task UpdateGameStateAsync(string maPhong, string trangThai)
    {
        try
        {
            // Cập nhật trạng thái trong game session
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession != null)
            {
                gameSession.IsGameActive = trangThai == GameFlowConstants.GameStates.Playing;
                if (trangThai == GameFlowConstants.GameStates.Ended)
                {
                    gameSession.IsGameEnded = true;
                }
            }
            // Cập nhật trạng thái trong game room
            if (_gameRooms.TryGetValue(maPhong, out var gameRoom))
            {
                gameRoom.GameState = trangThai;
            }
            // Broadcast trạng thái mới đến tất cả client
            await _eventBroadcaster.BroadcastGameStateChangedAsync(maPhong, trangThai);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Kết thúc game thủ công (do host)
    /// </summary>
    public async Task EndGameManuallyAsync(string maPhong, string lyDo = "Host kết thúc game")
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return;
            }
            // Kết thúc game session
            _sessionManager.EndGameSession(maPhong);
            // Tạo kết quả cuối cùng
            var ketQuaCuoiCung = gameSession.PlayerProgress.Values.Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count
            }).OrderByDescending(p => p.score).ToList<object>();
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new GameEndEventData
            {
                Reason = GameFlowConstants.EndReasons.HostEnded,
                Message = lyDo,
                FinalResults = ketQuaCuoiCung
            };
            await _eventBroadcaster.BroadcastGameEndedAsync(maPhong, duLieuSuKien);
            // Dọn dẹp sau delay
            _ = Task.Delay(TimeSpan.FromSeconds(GameFlowConstants.Defaults.CleanupDelaySeconds))
                .ContinueWith(async _ => await CleanupGameSessionAsync(maPhong));
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Dọn dẹp game session khi kết thúc
    /// </summary>
    public async Task CleanupGameSessionAsync(string maPhong)
    {
        try
        {
            // Dừng tất cả timer
            _timerManager.DisposeAllTimersForRoom(maPhong);
            // Cleanup game session
            _sessionManager.CleanupGameSession(maPhong);
            // Reset trạng thái phòng về "chờ"
            if (_gameRooms.TryGetValue(maPhong, out var gameRoom))
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
    public async Task PauseGameAsync(string maPhong)
    {
        try
        {
            await UpdateGameStateAsync(maPhong, "paused");
            // Dừng tất cả timer tạm thời
            _timerManager.DisposeAllTimersForRoom(maPhong);
            // Broadcast thông báo tạm dừng
            await _eventBroadcaster.BroadcastGameStateChangedAsync(maPhong, "paused");
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Tiếp tục game sau khi tạm dừng
    /// </summary>
    public async Task ResumeGameAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null) return;
            await UpdateGameStateAsync(maPhong, GameFlowConstants.GameStates.Playing);
            // Tính thời gian còn lại và tạo lại timer
            var thoiGianConLai = GetRemainingGameTime(gameSession);
            if (thoiGianConLai > 0)
            {
                _timerManager.CreateGameTimer(maPhong, thoiGianConLai, async () =>
                {
                    await EndGameDueToTimeoutAsync(maPhong);
                });
            }
            // Broadcast thông báo tiếp tục
            await _eventBroadcaster.BroadcastGameStateChangedAsync(maPhong, GameFlowConstants.GameStates.Playing);
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
    private async Task EndGameDueToTimeoutAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null) return;
            // Kết thúc game session
            _sessionManager.EndGameSession(maPhong);
            // Tạo kết quả cuối cùng
            var ketQuaCuoiCung = gameSession.PlayerProgress.Values.Select(p => new {
                username = p.Username,
                score = p.Score,
                answersCount = p.Answers.Count
            }).OrderByDescending(p => p.score).ToList<object>();
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new GameEndEventData
            {
                Reason = GameFlowConstants.EndReasons.Timeout,
                Message = GameFlowConstants.Messages.GameEndedTimeout,
                FinalResults = ketQuaCuoiCung
            };
            await _eventBroadcaster.BroadcastGameEndedAsync(maPhong, duLieuSuKien);
            // Dọn dẹp sau delay
            _ = Task.Delay(TimeSpan.FromSeconds(GameFlowConstants.Defaults.CleanupDelaySeconds))
                .ContinueWith(async _ => await CleanupGameSessionAsync(maPhong));
        }
        catch (Exception ex)
        {
        }
    }
    #endregion
}
