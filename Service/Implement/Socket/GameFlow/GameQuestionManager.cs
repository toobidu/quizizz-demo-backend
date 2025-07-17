using ConsoleApp1.Model.DTO.Game;
using System.Text.Json;

namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Quản lý câu hỏi trong game - Chịu trách nhiệm:
/// 1. Gửi câu hỏi cho người chơi cụ thể
/// 2. Broadcast câu hỏi cho tất cả người chơi
/// 3. Quản lý thứ tự câu hỏi
/// 4. Xử lý logic khi hết câu hỏi
/// </summary>
public class GameQuestionManager
{
    private readonly GameSessionManager _sessionManager;
    private readonly GameEventBroadcaster _eventBroadcaster;

    public GameQuestionManager(
        GameSessionManager sessionManager,
        GameEventBroadcaster eventBroadcaster)
    {
        _sessionManager = sessionManager;
        _eventBroadcaster = eventBroadcaster;
    }

    /// <summary>
    /// Gửi câu hỏi tiếp theo cho một người chơi cụ thể
    /// Dùng trong chế độ self-paced (mỗi người chơi với tốc độ riêng)
    /// </summary>
    public async Task GuiCauHoiTiepTheoChoNguoiChoiAsync(string maPhong, string tenNguoiChoi)
    {
        Console.WriteLine($"[CAUHOI] Đang gửi câu hỏi tiếp theo cho {tenNguoiChoi} trong phòng {maPhong}");
        
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                Console.WriteLine($"[CAUHOI] {GameFlowConstants.Messages.NoActiveSession} cho phòng {maPhong}");
                return;
            }

            if (!gameSession.PlayerProgress.TryGetValue(tenNguoiChoi, out var tienDoNguoiChoi))
            {
                Console.WriteLine($"[CAUHOI] {GameFlowConstants.Messages.PlayerNotFound}: {tenNguoiChoi}");
                return;
            }

            // Kiểm tra xem còn câu hỏi nào không
            if (tienDoNguoiChoi.CurrentQuestionIndex >= gameSession.Questions.Count)
            {
                Console.WriteLine($"[CAUHOI] Người chơi {tenNguoiChoi} đã hoàn thành tất cả câu hỏi");
                tienDoNguoiChoi.HasFinished = true;
                
                // Thông báo người chơi đã hoàn thành
                await _eventBroadcaster.SendPlayerFinishedAsync(maPhong, tenNguoiChoi, new {
                    message = GameFlowConstants.Messages.AllQuestionsCompleted,
                    finalScore = tienDoNguoiChoi.Score
                });
                
                // Kiểm tra xem tất cả người chơi đã hoàn thành chưa
                await KiemTraTatCaNguoiChoiHoanThanhAsync(maPhong);
                return;
            }

            // Lấy câu hỏi tiếp theo
            var cauHoiTiepTheo = gameSession.Questions[tienDoNguoiChoi.CurrentQuestionIndex];
            
            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new QuestionEventData
            {
                Question = cauHoiTiepTheo,
                QuestionIndex = tienDoNguoiChoi.CurrentQuestionIndex,
                TotalQuestions = gameSession.Questions.Count,
                TimeRemaining = LayThoiGianConLaiCuaGame(gameSession)
            };

            // Gửi câu hỏi cho người chơi
            await _eventBroadcaster.SendNextQuestionToPlayerAsync(maPhong, tenNguoiChoi, duLieuSuKien);

            // Cập nhật thời gian hoạt động cuối
            _sessionManager.UpdatePlayerProgress(maPhong, tenNguoiChoi, progress => 
            {
                progress.LastActivityTime = DateTime.UtcNow;
            });
            
            Console.WriteLine($"[CAUHOI] Đã gửi câu hỏi {tienDoNguoiChoi.CurrentQuestionIndex + 1}/{gameSession.Questions.Count} cho {tenNguoiChoi}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CAUHOI] Lỗi khi gửi câu hỏi tiếp theo cho {tenNguoiChoi}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi trong phòng
    /// Dùng trong chế độ synchronized (tất cả cùng câu hỏi)
    /// </summary>
    public async Task GuiCauHoiAsync(string maPhong, object cauHoi, int viTriCauHoi, int tongSoCauHoi)
    {
        Console.WriteLine($"[CAUHOI] Đang gửi câu hỏi {viTriCauHoi + 1}/{tongSoCauHoi} đến phòng {maPhong}");
        
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
                Console.WriteLine($"[CAUHOI] {GameFlowConstants.Messages.NoActiveSession} cho phòng {maPhong}");
                return;
            }

            // Cập nhật vị trí câu hỏi hiện tại của game session
            gameSession.CurrentQuestionIndex = viTriCauHoi;

            // Tạo dữ liệu sự kiện
            var duLieuSuKien = new QuestionEventData
            {
                Question = cauHoi,
                QuestionIndex = viTriCauHoi,
                TotalQuestions = tongSoCauHoi,
                TimeRemaining = LayThoiGianConLaiCuaGame(gameSession),
                GameState = GameFlowConstants.GameStates.QuestionActive
            };

            // Broadcast câu hỏi đến tất cả người chơi trong phòng
            await _eventBroadcaster.BroadcastNewQuestionAsync(maPhong, duLieuSuKien);

            Console.WriteLine($"[CAUHOI] Câu hỏi {viTriCauHoi + 1}/{tongSoCauHoi} đã được broadcast đến phòng {maPhong}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CAUHOI] Lỗi khi gửi câu hỏi đến phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse danh sách câu hỏi từ JSON object
    /// </summary>
    public List<QuestionData> ParseDanhSachCauHoi(object cauHoi)
    {
        try
        {
            var cauHoiJson = JsonSerializer.Serialize(cauHoi);
            var danhSachCauHoi = JsonSerializer.Deserialize<List<QuestionData>>(cauHoiJson) ?? new List<QuestionData>();
            
            Console.WriteLine($"[CAUHOI] Đã parse {danhSachCauHoi.Count} câu hỏi từ JSON");
            return danhSachCauHoi;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CAUHOI] Lỗi khi parse danh sách câu hỏi: {ex.Message}");
            return new List<QuestionData>();
        }
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của danh sách câu hỏi
    /// </summary>
    public bool KiemTraTinhHopLeCauHoi(List<QuestionData> danhSachCauHoi)
    {
        if (danhSachCauHoi.Count == 0)
        {
            Console.WriteLine("[CAUHOI] Danh sách câu hỏi trống");
            return false;
        }

        if (danhSachCauHoi.Count > GameFlowConstants.Limits.MaxQuestionsPerGame)
        {
            Console.WriteLine($"[CAUHOI] Số lượng câu hỏi vượt quá giới hạn: {danhSachCauHoi.Count}/{GameFlowConstants.Limits.MaxQuestionsPerGame}");
            return false;
        }

        // Kiểm tra từng câu hỏi có hợp lệ không
        for (int i = 0; i < danhSachCauHoi.Count; i++)
        {
            var cauHoi = danhSachCauHoi[i];
            if (string.IsNullOrEmpty(cauHoi.Question))
            {
                Console.WriteLine($"[CAUHOI] Câu hỏi thứ {i + 1} không có nội dung");
                return false;
            }
        }

        Console.WriteLine($"[CAUHOI] Danh sách {danhSachCauHoi.Count} câu hỏi hợp lệ");
        return true;
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
    /// Kiểm tra xem tất cả người chơi đã hoàn thành chưa
    /// </summary>
    private async Task KiemTraTatCaNguoiChoiHoanThanhAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null) return;

            var tatCaHoanThanh = gameSession.PlayerProgress.Values.All(p => p.HasFinished);
            if (tatCaHoanThanh && gameSession.IsGameActive)
            {
                Console.WriteLine($"[CAUHOI] Tất cả người chơi đã hoàn thành cho phòng {maPhong}");
                
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
                    Reason = GameFlowConstants.EndReasons.AllFinished,
                    Message = GameFlowConstants.Messages.GameEndedAllFinished,
                    FinalResults = ketQuaCuoiCung
                };
                
                await _eventBroadcaster.BroadcastGameEndedAsync(maPhong, duLieuSuKien);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CAUHOI] Lỗi khi kiểm tra tất cả người chơi hoàn thành cho phòng {maPhong}: {ex.Message}");
        }
    }

    #endregion
}