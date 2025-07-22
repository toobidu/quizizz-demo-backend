using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.Scoring;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
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
    // Dictionary lưu trữ các phòng game (chia sẻ với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    // Dictionary lưu trữ các kết nối WebSocket (chia sẻ với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    // Các thành phần
    private readonly ScoringSessionManager _sessionManager;
    private readonly ScoreCalculator _scoreCalculator;
    private readonly ScoreFormatter _scoreFormatter;
    private readonly SocketMessageSender _messageSender;
    public ScoringSocketServiceImplement(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
        _sessionManager = new ScoringSessionManager();
        _scoreCalculator = new ScoreCalculator();
        _scoreFormatter = new ScoreFormatter(_scoreCalculator);
        _messageSender = new SocketMessageSender(_gameRooms, _connections);
    }
    /// <summary>
    /// Cập nhật bảng điểm realtime
    /// Được gọi sau mỗi câu trả lời hoặc định kỳ
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="scoreboard">Bảng điểm hiện tại (JSON object)</param>
    public async Task UpdateScoreboardAsync(string roomCode, object scoreboard)
    {
        try
        {
            // Parse scoreboard data từ input
            var scoreboardData = _sessionManager.ParseScoreboardData(scoreboard);
            if (scoreboardData.Count == 0)
            {
                return;
            }
            // Cập nhật điểm số cho từng player
            _sessionManager.UpdatePlayerScores(roomCode, scoreboardData);
            // Lấy scoring session
            var scoringSession = _sessionManager.GetOrCreateSession(roomCode);
            // Tính toán bảng xếp hạng mới
            var newScoreboard = _scoreCalculator.CalculateScoreboard(scoringSession);
            // Detect thay đổi vị trí (ai lên/xuống hạng)
            var positionChanges = _scoreCalculator.DetectPositionChanges(scoringSession.CurrentScoreboard, newScoreboard);
            // Cập nhật scoreboard hiện tại
            scoringSession.CurrentScoreboard = newScoreboard;
            scoringSession.LastUpdateTime = DateTime.UtcNow;
            // Broadcast bảng điểm realtime đến tất cả client
            await _messageSender.BroadcastToRoomAsync(roomCode, ScoringConstants.Events.ScoreboardUpdated, new {
                scoreboard = newScoreboard,
                positionChanges = positionChanges,
                timestamp = DateTime.UtcNow,
                totalPlayers = newScoreboard.Count
            });
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Gửi kết quả cuối game đến tất cả người chơi
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="finalResults">Kết quả cuối game (JSON object)</param>
    public async Task SendFinalResultsAsync(string roomCode, object finalResults)
    {
        try
        {
            var scoringSession = _sessionManager.GetSession(roomCode);
            if (scoringSession == null)
            {
                return;
            }
            // Tính toán kết quả cuối cùng chi tiết
            var detailedResults = _scoreCalculator.CalculateFinalResults(scoringSession);
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
            await _messageSender.BroadcastToRoomAsync(roomCode, ScoringConstants.Events.FinalResults, combinedResults);
            // Gửi kết quả cá nhân cho từng player
            foreach (var playerScore in scoringSession.PlayerScores.Values)
            {
                var personalResult = _scoreFormatter.CreatePersonalResult(playerScore, detailedResults);
                await _messageSender.SendToPlayerAsync(roomCode, playerScore.Username, ScoringConstants.Events.PersonalFinalResult, personalResult);
            }
            // Đánh dấu game đã kết thúc
            scoringSession.IsGameActive = false;
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Kết thúc game và gửi kết quả final
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="finalResults">Kết quả cuối game</param>
    public async Task EndGameAsync(string roomCode, object finalResults)
    {
        try
        {
            // Dừng tất cả timer và game logic (nếu có)
            _sessionManager.EndGame(roomCode);
            // Tính toán kết quả cuối cùng
            await SendFinalResultsAsync(roomCode, finalResults);
            // Chuyển trạng thái phòng về "ended"
            if (_gameRooms.TryGetValue(roomCode, out var gameRoom))
            {
                gameRoom.GameState = ScoringConstants.GameStates.Finished;
            }
            // Broadcast thông báo game kết thúc
            await _messageSender.BroadcastToRoomAsync(roomCode, ScoringConstants.Events.GameEnded, new {
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
            _ = Task.Delay(TimeSpan.FromMinutes(ScoringConstants.Thresholds.SessionCleanupDelayMinutes)).ContinueWith(_ => {
                _sessionManager.CleanupSession(roomCode);
            });
        }
        catch (Exception ex)
        {
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
        try
        {
            // Format bảng điểm theo định dạng client mong đợi
            var formattedScoreboard = _scoreFormatter.FormatScoreboardForClient(scoreboard);
            // Gửi cho tất cả client trong phòng
            await _messageSender.BroadcastToRoomAsync(roomCode, ScoringConstants.Events.Scoreboard, new {
                scoreboard = formattedScoreboard,
                timestamp = DateTime.UtcNow,
                type = ScoringConstants.ScoreboardTypes.Current
            });
            // Có thể gửi cho từng client khác nhau (ví dụ: highlight vị trí của chính họ)
            var scoringSession = _sessionManager.GetSession(roomCode);
            if (scoringSession != null)
            {
                foreach (var playerScore in scoringSession.PlayerScores.Values)
                {
                    var personalizedScoreboard = _scoreFormatter.CreatePersonalizedScoreboard(formattedScoreboard, playerScore.Username);
                    await _messageSender.SendToPlayerAsync(roomCode, playerScore.Username, ScoringConstants.Events.PersonalScoreboard, personalizedScoreboard);
                }
            }
        }
        catch (Exception ex)
        {
        }
    }
}
