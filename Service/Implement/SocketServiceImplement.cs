using ConsoleApp1.Service.Interface;
using ConsoleApp1.Service.Interface.Socket;

namespace ConsoleApp1.Service.Implement;

/// <summary>
/// Composite WebSocket Service - Tổng hợp tất cả các service WebSocket con
/// Đây là service chính được inject vào controller và các service khác
/// Nó delegate (ủy quyền) tất cả method calls đến các service con tương ứng
/// </summary>
public class SocketServiceImplement : ISocketService
{
    // Các service con được tiêm thông qua constructor
    private readonly ISocketConnectionService _connectionService;           // Quản lý kết nối WebSocket
    private readonly IRoomManagementSocketService _roomManagementService;   // Quản lý phòng chơi
    private readonly IGameFlowSocketService _gameFlowService;               // Luồng game (câu hỏi, timer)
    private readonly IPlayerInteractionSocketService _playerInteractionService; // Tương tác người chơi
    private readonly IScoringSocketService _scoringService;                 // Tính điểm và bảng xếp hạng
    private readonly IHostControlSocketService _hostControlService;         // Điều khiển host

    /// <summary>
    /// Constructor - Nhận tất cả các service con thông qua tiêm phụ thuộc
    /// </summary>
    public SocketServiceImplement(
        ISocketConnectionService connectionService,
        IRoomManagementSocketService roomManagementService,
        IGameFlowSocketService gameFlowService,
        IPlayerInteractionSocketService playerInteractionService,
        IScoringSocketService scoringService,
        IHostControlSocketService hostControlService)
    {
        _connectionService = connectionService;
        _roomManagementService = roomManagementService;
        _gameFlowService = gameFlowService;
        _playerInteractionService = playerInteractionService;
        _scoringService = scoringService;
        _hostControlService = hostControlService;
    }

    #region ISocketConnectionService - Quản lý kết nối WebSocket
    /// <summary>
    /// Khởi động WebSocket server trên port được chỉ định
    /// </summary>
    public async Task StartAsync(int port) => await _connectionService.StartAsync(port);
    
    /// <summary>
    /// Dừng WebSocket server và đóng tất cả kết nối
    /// </summary>
    public async Task StopAsync() => await _connectionService.StopAsync();
    #endregion

    #region IRoomManagementSocketService - Quản lý phòng chơi
    /// <summary>
    /// Xử lý khi người chơi tham gia phòng
    /// </summary>
    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId) 
        => await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
    
    /// <summary>
    /// Xử lý khi người chơi rời phòng
    /// </summary>
    public async Task LeaveRoomAsync(string socketId, string roomCode) 
        => await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
    
    /// <summary>
    /// Cập nhật danh sách người chơi trong phòng cho tất cả client
    /// </summary>
    public async Task UpdateRoomPlayersAsync(string roomCode) 
        => await _roomManagementService.UpdateRoomPlayersAsync(roomCode);
    
    /// <summary>
    /// Broadcast sự kiện player-joined trực tiếp từ database data
    /// </summary>
    public async Task BroadcastPlayerJoinedEventAsync(string roomCode, int userId, string username)
        => await _roomManagementService.BroadcastPlayerJoinedEventAsync(roomCode, userId, username);
    #endregion

    #region IGameFlowSocketService - Luồng game và câu hỏi
    /// <summary>
    /// Bắt đầu game trong phòng
    /// </summary>
    public async Task StartGameAsync(string roomCode) => await _gameFlowService.StartGameAsync(roomCode);
    
    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit) 
        => await _gameFlowService.StartGameWithQuestionsAsync(roomCode, questions, gameTimeLimit);
    
    /// <summary>
    /// Gửi câu hỏi tiếp theo cho một người chơi cụ thể
    /// </summary>
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username) 
        => await _gameFlowService.SendNextQuestionToPlayerAsync(roomCode, username);
    
    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi trong phòng
    /// </summary>
    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions) 
        => await _gameFlowService.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
    
    /// <summary>
    /// Gửi cập nhật thời gian game còn lại
    /// </summary>
    public async Task SendGameTimerUpdateAsync(string roomCode) 
        => await _gameFlowService.SendGameTimerUpdateAsync(roomCode);
    
    /// <summary>
    /// Lấy tiến độ của một người chơi cụ thể
    /// </summary>
    public async Task GetPlayerProgressAsync(string roomCode, string username) 
        => await _gameFlowService.GetPlayerProgressAsync(roomCode, username);
    
    /// <summary>
    /// Broadcast tiến độ của tất cả người chơi
    /// </summary>
    public async Task BroadcastPlayerProgressAsync(string roomCode) 
        => await _gameFlowService.BroadcastPlayerProgressAsync(roomCode);
    
    /// <summary>
    /// Dọn dẹp session game khi kết thúc
    /// </summary>
    public async Task CleanupGameSessionAsync(string roomCode) 
        => await _gameFlowService.CleanupGameSessionAsync(roomCode);
    
    /// <summary>
    /// Cập nhật trạng thái game (waiting, playing, ended)
    /// </summary>
    public async Task UpdateGameStateAsync(string roomCode, string gameState) 
        => await _gameFlowService.UpdateGameStateAsync(roomCode, gameState);
    
    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    public async Task SendCountdownAsync(string roomCode, int countdown) 
        => await _gameFlowService.SendCountdownAsync(roomCode, countdown);
    #endregion

    #region IPlayerInteractionSocketService - Tương tác người chơi
    /// <summary>
    /// Nhận câu trả lời từ người chơi
    /// </summary>
    public async Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp) 
        => await _playerInteractionService.ReceiveAnswerAsync(roomCode, username, answer, timestamp);
    
    /// <summary>
    /// Cập nhật trạng thái người chơi (online, offline, answering)
    /// </summary>
    public async Task UpdatePlayerStatusAsync(string roomCode, string username, string status) 
        => await _playerInteractionService.UpdatePlayerStatusAsync(roomCode, username, status);
    #endregion

    #region IScoringSocketService - Tính điểm và bảng xếp hạng
    /// <summary>
    /// Cập nhật bảng điểm realtime
    /// </summary>
    public async Task UpdateScoreboardAsync(string roomCode, object scoreboard) 
        => await _scoringService.UpdateScoreboardAsync(roomCode, scoreboard);
    
    /// <summary>
    /// Gửi kết quả cuối game
    /// </summary>
    public async Task SendFinalResultsAsync(string roomCode, object finalResults) 
        => await _scoringService.SendFinalResultsAsync(roomCode, finalResults);
    
    /// <summary>
    /// Kết thúc game và gửi kết quả final
    /// </summary>
    public async Task EndGameAsync(string roomCode, object finalResults) 
        => await _scoringService.EndGameAsync(roomCode, finalResults);
    
    /// <summary>
    /// Gửi bảng điểm hiện tại
    /// </summary>
    public async Task SendScoreboardAsync(string roomCode, object scoreboard) 
        => await _scoringService.SendScoreboardAsync(roomCode, scoreboard);
    #endregion

    #region IHostControlSocketService - Điều khiển host
    /// <summary>
    /// Gửi thông báo chỉ cho host của phòng
    /// </summary>
    public async Task NotifyHostOnlyAsync(string roomCode, string message) 
        => await _hostControlService.NotifyHostOnlyAsync(roomCode, message);
    
    /// <summary>
    /// Host yêu cầu câu hỏi tiếp theo
    /// </summary>
    public async Task RequestNextQuestionAsync(string roomCode) 
        => await _hostControlService.RequestNextQuestionAsync(roomCode);
    #endregion
}