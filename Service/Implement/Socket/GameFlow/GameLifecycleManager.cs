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
    public async Task BatDauGameDonGianAsync(string maPhong)
    {
        Console.WriteLine($"[VONGDOI] Đang bắt đầu game đơn giản cho phòng: {maPhong}");
        
        try
        {
            // Kiểm tra phòng có tồn tại không
            if (!_gameRooms.ContainsKey(maPhong))
            {
                Console.WriteLine($"[VONGDOI] Không tìm thấy phòng {maPhong}");
                return;
            }

            var gameRoom = _gameRooms[maPhong];
            
            // Kiểm tra có đủ người chơi không (tối thiểu 1 người)
            if (gameRoom.Players.Count == 0)
            {
                Console.WriteLine($"[VONGDOI] Phòng {maPhong} không có người chơi nào");
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

            Console.WriteLine($"[VONGDOI] Game đơn giản đã bắt đầu cho phòng {maPhong} với {gameRoom.Players.Count} người chơi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi bắt đầu game đơn giản cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    public async Task BatDauGameVoiCauHoiAsync(string maPhong, object cauHoi, int thoiGianGioiHan)
    {
        Console.WriteLine($"[VONGDOI] Đang bắt đầu game với câu hỏi cho phòng: {maPhong}, thời gian: {thoiGianGioiHan}s");
        
        try
        {
            // Kiểm tra thời gian giới hạn hợp lệ
            if (!KiemTraThoiGianGioiHanHopLe(thoiGianGioiHan))
            {
                Console.WriteLine($"[VONGDOI] Thời gian giới hạn không hợp lệ: {thoiGianGioiHan}s");
                return;
            }

            // Parse danh sách câu hỏi từ JSON
            var cauHoiJson = JsonSerializer.Serialize(cauHoi);
            var danhSachCauHoi = JsonSerializer.Deserialize<List<QuestionData>>(cauHoiJson) ?? new List<QuestionData>();
            
            if (danhSachCauHoi.Count == 0)
            {
                Console.WriteLine($"[VONGDOI] {GameFlowConstants.Messages.NoQuestions} cho phòng {maPhong}");
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
            await GuiDemNguocAsync(maPhong, GameFlowConstants.Defaults.CountdownSeconds);
            
            // Khởi tạo timer cho game (tự động kết thúc khi hết thời gian)
            _timerManager.CreateGameTimer(maPhong, thoiGianGioiHan, async () =>
            {
                await KetThucGameDoHetThoiGianAsync(maPhong);
            });

            Console.WriteLine($"[VONGDOI] Game với {danhSachCauHoi.Count} câu hỏi đã bắt đầu cho phòng {maPhong}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi bắt đầu game với câu hỏi cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    public async Task GuiDemNguocAsync(string maPhong, int demNguoc)
    {
        Console.WriteLine($"[VONGDOI] Đang gửi đếm ngược {demNguoc} cho phòng {maPhong}");
        
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                Console.WriteLine($"[VONGDOI] {GameFlowConstants.Messages.NoActiveSession} cho phòng {maPhong}");
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
                            TimeRemaining = LayThoiGianConLaiCuaGame(gameSession),
                            GameState = GameFlowConstants.GameStates.QuestionActive
                        };
                        await _eventBroadcaster.BroadcastNewQuestionAsync(maPhong, duLieuCauHoi);
                    }
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi gửi đếm ngược cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật trạng thái game
    /// </summary>
    public async Task CapNhatTrangThaiGameAsync(string maPhong, string trangThai)
    {
        Console.WriteLine($"[VONGDOI] Đang cập nhật trạng thái game cho phòng {maPhong} thành: {trangThai}");
        
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
            Console.WriteLine($"[VONGDOI] Lỗi khi cập nhật trạng thái game cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Kết thúc game thủ công (do host)
    /// </summary>
    public async Task KetThucGameThucCongAsync(string maPhong, string lyDo = "Host kết thúc game")
    {
        Console.WriteLine($"[VONGDOI] Đang kết thúc game thủ công cho phòng {maPhong}: {lyDo}");
        
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                Console.WriteLine($"[VONGDOI] Không tìm thấy game session cho phòng {maPhong}");
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
                .ContinueWith(async _ => await DonDepGameSessionAsync(maPhong));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi kết thúc game thủ công cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Dọn dẹp game session khi kết thúc
    /// </summary>
    public async Task DonDepGameSessionAsync(string maPhong)
    {
        Console.WriteLine($"[VONGDOI] Đang dọn dẹp game session cho phòng {maPhong}");
        
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

            Console.WriteLine($"[VONGDOI] Game session đã được dọn dẹp cho phòng {maPhong}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi dọn dẹp game session cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Tạm dừng game
    /// </summary>
    public async Task TamDungGameAsync(string maPhong)
    {
        Console.WriteLine($"[VONGDOI] Đang tạm dừng game cho phòng {maPhong}");
        
        try
        {
            await CapNhatTrangThaiGameAsync(maPhong, "paused");
            
            // Dừng tất cả timer tạm thời
            _timerManager.DisposeAllTimersForRoom(maPhong);
            
            // Broadcast thông báo tạm dừng
            await _eventBroadcaster.BroadcastGameStateChangedAsync(maPhong, "paused");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi tạm dừng game cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Tiếp tục game sau khi tạm dừng
    /// </summary>
    public async Task TiepTucGameAsync(string maPhong)
    {
        Console.WriteLine($"[VONGDOI] Đang tiếp tục game cho phòng {maPhong}");
        
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null) return;

            await CapNhatTrangThaiGameAsync(maPhong, GameFlowConstants.GameStates.Playing);
            
            // Tính thời gian còn lại và tạo lại timer
            var thoiGianConLai = LayThoiGianConLaiCuaGame(gameSession);
            if (thoiGianConLai > 0)
            {
                _timerManager.CreateGameTimer(maPhong, thoiGianConLai, async () =>
                {
                    await KetThucGameDoHetThoiGianAsync(maPhong);
                });
            }
            
            // Broadcast thông báo tiếp tục
            await _eventBroadcaster.BroadcastGameStateChangedAsync(maPhong, GameFlowConstants.GameStates.Playing);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi tiếp tục game cho phòng {maPhong}: {ex.Message}");
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Kiểm tra thời gian giới hạn có hợp lệ không
    /// </summary>
    private bool KiemTraThoiGianGioiHanHopLe(int thoiGianGioiHan)
    {
        return thoiGianGioiHan >= GameFlowConstants.Limits.MinGameTimeLimit && 
               thoiGianGioiHan <= GameFlowConstants.Limits.MaxGameTimeLimit;
    }

    /// <summary>
    /// Tính thời gian còn lại của game (giây)
    /// </summary>
    private int LayThoiGianConLaiCuaGame(GameSession gameSession)
    {
        if (!gameSession.IsGameActive) return 0;
        
        var thoiGianDaTroi = (DateTime.UtcNow - gameSession.GameStartTime).TotalSeconds;
        var thoiGianConLai = gameSession.GameTimeLimit - thoiGianDaTroi;
        return Math.Max(0, (int)thoiGianConLai);
    }

    /// <summary>
    /// Kết thúc game do hết thời gian
    /// </summary>
    private async Task KetThucGameDoHetThoiGianAsync(string maPhong)
    {
        Console.WriteLine($"[VONGDOI] Game kết thúc do hết thời gian cho phòng {maPhong}");
        
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
                .ContinueWith(async _ => await DonDepGameSessionAsync(maPhong));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[VONGDOI] Lỗi khi kết thúc game do hết thời gian cho phòng {maPhong}: {ex.Message}");
        }
    }

    #endregion
}