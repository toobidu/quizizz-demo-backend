namespace ConsoleApp1.Service.Implement.Socket.GameFlow;
/// <summary>
/// Theo dõi tiến độ game - Chịu trách nhiệm:
/// 1. Theo dõi tiến độ từng người chơi
/// 2. Cập nhật thời gian game realtime
/// 3. Broadcast leaderboard
/// 4. Tính toán thống kê game
/// </summary>
public class GameProgressTracker
{
    private readonly GameSessionManager _sessionManager;
    private readonly GameEventBroadcaster _eventBroadcaster;
    public GameProgressTracker(
        GameSessionManager sessionManager,
        GameEventBroadcaster eventBroadcaster)
    {
        _sessionManager = sessionManager;
        _eventBroadcaster = eventBroadcaster;
    }
    /// <summary>
    /// Gửi cập nhật thời gian game còn lại
    /// Được gọi định kỳ bởi timer
    /// </summary>
    public async Task GuiCapNhatThoiGianGameAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return;
            }
            var thoiGianConLai = LayThoiGianConLaiCuaGame(gameSession);
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new TimerUpdateEventData
            {
                TimeRemaining = thoiGianConLai,
                TotalTime = gameSession.GameTimeLimit,
                GameState = gameSession.IsGameActive ? GameFlowConstants.GameStates.Playing : GameFlowConstants.GameStates.Ended
            };
            // Broadcast thời gian còn lại đến tất cả client
            await _eventBroadcaster.BroadcastTimerUpdateAsync(maPhong, duLieuSuKien);
            // Nếu hết thời gian thì kết thúc game
            if (thoiGianConLai <= 0 && gameSession.IsGameActive)
            {
                await KetThucGameDoHetThoiGianAsync(maPhong);
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Lấy tiến độ của một người chơi cụ thể
    /// </summary>
    public async Task LayTienDoNguoiChoiAsync(string maPhong, string tenNguoiChoi)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return;
            }
            if (!gameSession.PlayerProgress.TryGetValue(tenNguoiChoi, out var tienDoNguoiChoi))
            {
                return;
            }
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new PlayerProgressEventData
            {
                CurrentQuestionIndex = tienDoNguoiChoi.CurrentQuestionIndex,
                TotalQuestions = gameSession.Questions.Count,
                Score = tienDoNguoiChoi.Score,
                AnswersCount = tienDoNguoiChoi.Answers.Count,
                HasFinished = tienDoNguoiChoi.HasFinished,
                TimeRemaining = LayThoiGianConLaiCuaGame(gameSession)
            };
            // Gửi thông tin tiến độ cho người chơi
            await _eventBroadcaster.SendPlayerProgressAsync(maPhong, tenNguoiChoi, duLieuSuKien);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Broadcast tiến độ của tất cả người chơi
    /// Để hiển thị realtime leaderboard
    /// </summary>
    public async Task BroadcastTienDoNguoiChoiAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return;
            }
            // Thu thập tiến độ của tất cả người chơi
            var danhSachTienDo = gameSession.PlayerProgress.Values
                .Select(p => new {
                    username = p.Username,
                    score = p.Score,
                    currentQuestion = p.CurrentQuestionIndex + 1,
                    totalQuestions = gameSession.Questions.Count,
                    hasFinished = p.HasFinished,
                    answersCount = p.Answers.Count
                })
                .OrderByDescending(p => p.score) // Sắp xếp theo điểm số
                .ToList<object>();
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new ProgressUpdateEventData
            {
                Players = danhSachTienDo,
                GameState = gameSession.IsGameActive ? GameFlowConstants.GameStates.Playing : GameFlowConstants.GameStates.Ended,
                TimeRemaining = LayThoiGianConLaiCuaGame(gameSession)
            };
            // Broadcast leaderboard realtime
            await _eventBroadcaster.BroadcastProgressUpdateAsync(maPhong, duLieuSuKien);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Cập nhật điểm số cho người chơi
    /// </summary>
    public void CapNhatDiemSoNguoiChoi(string maPhong, string tenNguoiChoi, int diemSo)
    {
        try
        {
            _sessionManager.UpdatePlayerProgress(maPhong, tenNguoiChoi, progress =>
            {
                progress.Score += diemSo;
                progress.LastActivityTime = DateTime.UtcNow;
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Cập nhật vị trí câu hỏi hiện tại của người chơi
    /// </summary>
    public void CapNhatViTriCauHoiNguoiChoi(string maPhong, string tenNguoiChoi, int viTriCauHoi)
    {
        try
        {
            _sessionManager.UpdatePlayerProgress(maPhong, tenNguoiChoi, progress =>
            {
                progress.CurrentQuestionIndex = viTriCauHoi;
                progress.LastActivityTime = DateTime.UtcNow;
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Lấy thống kê game hiện tại
    /// </summary>
    public object LayThongKeGame(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                return new { error = "Không tìm thấy game session" };
            }
            var tongNguoiChoi = gameSession.PlayerProgress.Count;
            var nguoiChoiHoanThanh = gameSession.PlayerProgress.Values.Count(p => p.HasFinished);
            var diemSoTrungBinh = gameSession.PlayerProgress.Values.Any() 
                ? gameSession.PlayerProgress.Values.Average(p => p.Score) 
                : 0;
            var thoiGianConLai = LayThoiGianConLaiCuaGame(gameSession);
            return new
            {
                tongNguoiChoi,
                nguoiChoiHoanThanh,
                nguoiChoiDangChoi = tongNguoiChoi - nguoiChoiHoanThanh,
                diemSoTrungBinh = Math.Round(diemSoTrungBinh, 2),
                thoiGianConLai,
                trangThaiGame = gameSession.IsGameActive ? "Đang chơi" : "Đã kết thúc",
                tongCauHoi = gameSession.Questions.Count,
                cauHoiHienTai = gameSession.CurrentQuestionIndex + 1
            };
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }
    #region Private Helper Methods
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
        }
        catch (Exception ex)
        {
        }
    }
    #endregion
}
