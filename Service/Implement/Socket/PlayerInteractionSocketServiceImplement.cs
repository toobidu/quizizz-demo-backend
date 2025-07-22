using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.PlayerInteraction;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
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
    // Dictionary lưu trữ các phòng game (chia sẻ với các service khác)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    // Dictionary lưu trữ các kết nối WebSocket (chia sẻ với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    // Dictionary lưu trữ game sessions (chia sẻ với GameFlowService)
    private readonly ConcurrentDictionary<string, PlayerGameSession> _gameSessions = new();
    // Các thành phần
    private readonly PlayerGameSessionManager _sessionManager;
    private readonly AnswerProcessor _answerProcessor;
    private readonly PlayerStatusManager _statusManager;
    private readonly PlayerInteractionEventBroadcaster _eventBroadcaster;
    public PlayerInteractionSocketServiceImplement(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;
        _sessionManager = new PlayerGameSessionManager(_gameSessions);
        _answerProcessor = new AnswerProcessor();
        _statusManager = new PlayerStatusManager(_gameSessions, _gameRooms);
        _eventBroadcaster = new PlayerInteractionEventBroadcaster(_gameRooms, _connections);
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
        try
        {
            // Xác thực phiên game
            var gameSession = _sessionManager.GetGameSession(roomCode);
            if (gameSession == null)
            {
                await _eventBroadcaster.SendErrorToPlayerAsync(roomCode, username, PlayerInteractionConstants.Messages.NoActiveSession);
                return;
            }
            if (!gameSession.IsGameActive)
            {
                await _eventBroadcaster.SendErrorToPlayerAsync(roomCode, username, PlayerInteractionConstants.Messages.GameEnded);
                return;
            }
            // Phân tích câu trả lời được gửi
            var playerAnswer = _sessionManager.ParseAnswerSubmission(answer);
            if (playerAnswer == null)
            {
                await _eventBroadcaster.SendErrorToPlayerAsync(roomCode, username, PlayerInteractionConstants.Messages.InvalidAnswerFormat);
                return;
            }
            // Lấy kết quả người chơi
            var playerResult = _sessionManager.GetOrCreatePlayerResult(roomCode, username);
            // Xác thực câu trả lời được gửi
            var (isValid, errorMessage) = _answerProcessor.ValidateAnswerSubmission(playerAnswer, gameSession, playerResult);
            if (!isValid)
            {
                await _eventBroadcaster.SendErrorToPlayerAsync(roomCode, username, errorMessage);
                return;
            }
            // Lấy câu hỏi
            var question = gameSession.Questions[playerAnswer.QuestionIndex];
            // Xử lý câu trả lời
            var answerRecord = _answerProcessor.ProcessAnswer(playerAnswer, question, gameSession.GameStartTime, username);
            // Cập nhật kết quả người chơi
            _sessionManager.UpdatePlayerResult(roomCode, username, result =>
            {
                result.Answers.Add(answerRecord);
                result.Score += answerRecord.PointsEarned;
                result.LastAnswerTime = DateTime.UtcNow;
                result.Status = PlayerInteractionConstants.PlayerStatuses.Answered;
            });
            // Gửi kết quả câu trả lời cho người chơi
            var answerResultData = _answerProcessor.CreateAnswerResultEventData(answerRecord, question, playerResult.Score);
            await _eventBroadcaster.SendAnswerResultAsync(roomCode, username, answerResultData);
            // Phát sóng cập nhật điểm số
            await BroadcastScoreUpdateAsync(roomCode);
            // Kiểm tra hoàn thành câu hỏi
            if (_statusManager.CheckQuestionCompletion(roomCode, playerAnswer.QuestionIndex))
            {
                await _eventBroadcaster.BroadcastQuestionCompletedAsync(roomCode, playerAnswer.QuestionIndex);
            }
            // Kiểm tra xem người chơi đã hoàn thành tất cả câu hỏi chưa
            if (playerResult.Answers.Count >= gameSession.Questions.Count)
            {
                _statusManager.MarkPlayerFinished(roomCode, username);
                var finishedEventData = new PlayerFinishedEventData
                {
                    Message = PlayerInteractionConstants.Messages.PlayerFinished,
                    FinalScore = playerResult.Score,
                    TotalQuestions = gameSession.Questions.Count
                };
                await _eventBroadcaster.SendPlayerFinishedAsync(roomCode, username, finishedEventData);
                // Kiểm tra xem tất cả người chơi đã hoàn thành chưa
                await CheckAllPlayersFinishedAsync(roomCode);
            }
        }
        catch (Exception ex)
        {
            await _eventBroadcaster.SendErrorToPlayerAsync(roomCode, username, PlayerInteractionConstants.Messages.AnswerProcessingError);
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
        try
        {
            // Cập nhật trạng thái người chơi
            var success = _statusManager.UpdatePlayerStatus(roomCode, username, status);
            if (!success)
            {
                return;
            }
            // Phát sóng thay đổi trạng thái
            var statusEventData = new PlayerStatusEventData
            {
                Username = username,
                Status = status,
                Timestamp = DateTime.UtcNow
            };
            await _eventBroadcaster.BroadcastPlayerStatusChangeAsync(roomCode, statusEventData);
            // Xử lý logic theo trạng thái cụ thể
            await HandleStatusChange(roomCode, username, status);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Xử lý logic thay đổi trạng thái
    /// </summary>
    private async Task HandleStatusChange(string roomCode, string username, string status)
    {
        switch (status.ToLower())
        {
            case PlayerInteractionConstants.PlayerStatuses.Online:
                break;
            case PlayerInteractionConstants.PlayerStatuses.Offline:
                break;
            case PlayerInteractionConstants.PlayerStatuses.Answering:
                break;
            case PlayerInteractionConstants.PlayerStatuses.Waiting:
                break;
            case PlayerInteractionConstants.PlayerStatuses.Finished:
                await CheckAllPlayersFinishedAsync(roomCode);
                break;
        }
    }
    /// <summary>
    /// Broadcast cập nhật điểm số cho tất cả player
    /// </summary>
    private async Task BroadcastScoreUpdateAsync(string roomCode)
    {
        try
        {
            var scoreboard = _sessionManager.CreateScoreboard(roomCode);
            var eventData = new ScoreboardUpdateEventData
            {
                Scoreboard = scoreboard,
                Timestamp = DateTime.UtcNow
            };
            await _eventBroadcaster.BroadcastScoreboardUpdateAsync(roomCode, eventData);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Kiểm tra xem tất cả player đã hoàn thành game chưa
    /// </summary>
    private async Task CheckAllPlayersFinishedAsync(string roomCode)
    {
        try
        {
            if (_statusManager.CheckAllPlayersFinished(roomCode))
            {
                // Kết thúc phiên game
                _sessionManager.EndGameSession(roomCode);
                // Tạo kết quả cuối cùng
                var finalResults = _sessionManager.CreateFinalResults(roomCode);
                var eventData = new GameCompletionEventData
                {
                    Reason = PlayerInteractionConstants.CompletionReasons.AllFinished,
                    Message = PlayerInteractionConstants.Messages.AllPlayersFinished,
                    FinalResults = finalResults
                };
                await _eventBroadcaster.BroadcastGameCompletedAsync(roomCode, eventData);
            }
        }
        catch (Exception ex)
        {
        }
    }
}
