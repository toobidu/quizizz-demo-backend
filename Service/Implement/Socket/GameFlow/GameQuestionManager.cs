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
    public async Task SendNextQuestionToPlayerAsync(string maPhong, string tenNguoiChoi)
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
            // Kiểm tra xem còn câu hỏi nào không
            if (tienDoNguoiChoi.CurrentQuestionIndex >= gameSession.Questions.Count)
            {
                tienDoNguoiChoi.HasFinished = true;
                // Thông báo người chơi đã hoàn thành
                await _eventBroadcaster.SendPlayerFinishedAsync(maPhong, tenNguoiChoi, new {
                    message = GameFlowConstants.Messages.AllQuestionsCompleted,
                    finalScore = tienDoNguoiChoi.Score
                });
                // Kiểm tra xem tất cả người chơi đã hoàn thành chưa
                await CheckAllPlayersFinishedAsync(maPhong);
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
                TimeRemaining = GetRemainingGameTime(gameSession)
            };
            // Gửi câu hỏi cho người chơi
            await _eventBroadcaster.SendNextQuestionToPlayerAsync(maPhong, tenNguoiChoi, duLieuSuKien);
            // Cập nhật thời gian hoạt động cuối
            _sessionManager.UpdatePlayerProgress(maPhong, tenNguoiChoi, progress => 
            {
                progress.LastActivityTime = DateTime.UtcNow;
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi trong phòng
    /// Dùng trong chế độ synchronized (tất cả cùng câu hỏi)
    /// </summary>
    public async Task SendQuestionAsync(string maPhong, object cauHoi, int viTriCauHoi, int tongSoCauHoi)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null)
            {
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
                TimeRemaining = GetRemainingGameTime(gameSession),
                GameState = GameFlowConstants.GameStates.QuestionActive
            };
            // Broadcast câu hỏi đến tất cả người chơi trong phòng
            await _eventBroadcaster.BroadcastNewQuestionAsync(maPhong, duLieuSuKien);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Parse danh sách câu hỏi từ JSON object
    /// </summary>
    public List<QuestionData> ParseQuestionList(object cauHoi)
    {
        try
        {
            var cauHoiJson = JsonSerializer.Serialize(cauHoi);
            var danhSachCauHoi = JsonSerializer.Deserialize<List<QuestionData>>(cauHoiJson) ?? new List<QuestionData>();
            return danhSachCauHoi;
        }
        catch (Exception ex)
        {
            return new List<QuestionData>();
        }
    }
    /// <summary>
    /// Kiểm tra tính hợp lệ của danh sách câu hỏi
    /// </summary>
    public bool ValidateQuestions(List<QuestionData> danhSachCauHoi)
    {
        if (danhSachCauHoi.Count == 0)
        {
            return false;
        }
        if (danhSachCauHoi.Count > GameFlowConstants.Limits.MaxQuestionsPerGame)
        {
            return false;
        }
        // Kiểm tra từng câu hỏi có hợp lệ không
        for (int i = 0; i < danhSachCauHoi.Count; i++)
        {
            var cauHoi = danhSachCauHoi[i];
            if (string.IsNullOrEmpty(cauHoi.Question))
            {
                return false;
            }
        }
        return true;
    }
    #region Private Helper Methods
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
    /// Kiểm tra xem tất cả người chơi đã hoàn thành chưa
    /// </summary>
    private async Task CheckAllPlayersFinishedAsync(string maPhong)
    {
        try
        {
            var gameSession = _sessionManager.GetGameSession(maPhong);
            if (gameSession == null) return;
            var tatCaHoanThanh = gameSession.PlayerProgress.Values.All(p => p.HasFinished);
            if (tatCaHoanThanh && gameSession.IsGameActive)
            {
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
        }
    }
    #endregion
}
