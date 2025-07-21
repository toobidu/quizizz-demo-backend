using ConsoleApp1.Service.Interface;
using ConsoleApp1.Service.Interface.Socket;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Template cho Composite Socket Service - Đây là file mẫu
/// File thực tế đang sử dụng là SocketServiceImplement.cs ở thư mục cha
/// </summary>
public class CompositeSocketServiceTemplate : ISocketService
{
    private readonly Interface.Socket.ISocketConnectionService _connectionService;
    private readonly IRoomManagementSocketService _roomManagementService;
    private readonly IGameFlowSocketService _gameFlowService;
    private readonly IPlayerInteractionSocketService _playerInteractionService;
    private readonly IScoringSocketService _scoringService;
    private readonly IHostControlSocketService _hostControlService;

    public CompositeSocketServiceTemplate(
        Interface.Socket.ISocketConnectionService connectionService,
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

    // ISocketConnectionService
    public async Task StartAsync(int port) => await _connectionService.StartAsync(port);
    public async Task StopAsync() => await _connectionService.StopAsync();

    // IRoomManagementSocketService
    public async Task JoinRoomAsync(string socketId, string roomCode, string username, int userId) 
        => await _roomManagementService.JoinRoomAsync(socketId, roomCode, username, userId);
    public async Task LeaveRoomAsync(string socketId, string roomCode) 
        => await _roomManagementService.LeaveRoomAsync(socketId, roomCode);
    public async Task LeaveRoomByUserIdAsync(int userId, string roomCode)
        => await _roomManagementService.LeaveRoomByUserIdAsync(userId, roomCode);
    public async Task UpdateRoomPlayersAsync(string roomCode) 
        => await _roomManagementService.UpdateRoomPlayersAsync(roomCode);
    public async Task BroadcastPlayerJoinedEventAsync(string roomCode, int userId, string username)
        => await _roomManagementService.BroadcastPlayerJoinedEventAsync(roomCode, userId, username);
    public async Task BroadcastPlayerLeftEventAsync(string roomCode, int userId, string username)
        => await _roomManagementService.BroadcastPlayerLeftEventAsync(roomCode, userId, username);
    public async Task BroadcastToAllConnectionsAsync(string roomCode, string eventName, object data)
        => await _roomManagementService.BroadcastToAllConnectionsAsync(roomCode, eventName, data);
    public async Task RequestPlayersUpdateAsync(string socketId, string roomCode)
        => await _roomManagementService.RequestPlayersUpdateAsync(socketId, roomCode);
    public async Task<ConsoleApp1.Model.DTO.Game.GameRoom?> GetRoomAsync(string roomCode)
        => await _roomManagementService.GetRoomAsync(roomCode);

    // IGameFlowSocketService
    public async Task StartGameAsync(string roomCode) => await _gameFlowService.StartGameAsync(roomCode);
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit) 
        => await _gameFlowService.StartGameWithQuestionsAsync(roomCode, questions, gameTimeLimit);
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username) 
        => await _gameFlowService.SendNextQuestionToPlayerAsync(roomCode, username);
    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions) 
        => await _gameFlowService.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
    public async Task SendGameTimerUpdateAsync(string roomCode) 
        => await _gameFlowService.SendGameTimerUpdateAsync(roomCode);
    public async Task GetPlayerProgressAsync(string roomCode, string username) 
        => await _gameFlowService.GetPlayerProgressAsync(roomCode, username);
    public async Task BroadcastPlayerProgressAsync(string roomCode) 
        => await _gameFlowService.BroadcastPlayerProgressAsync(roomCode);
    public async Task CleanupGameSessionAsync(string roomCode) 
        => await _gameFlowService.CleanupGameSessionAsync(roomCode);
    public async Task UpdateGameStateAsync(string roomCode, string gameState) 
        => await _gameFlowService.UpdateGameStateAsync(roomCode, gameState);
    public async Task SendCountdownAsync(string roomCode, int countdown) 
        => await _gameFlowService.SendCountdownAsync(roomCode, countdown);

    // IPlayerInteractionSocketService
    public async Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp) 
        => await _playerInteractionService.ReceiveAnswerAsync(roomCode, username, answer, timestamp);
    public async Task UpdatePlayerStatusAsync(string roomCode, string username, string status) 
        => await _playerInteractionService.UpdatePlayerStatusAsync(roomCode, username, status);

    // IScoringSocketService
    public async Task UpdateScoreboardAsync(string roomCode, object scoreboard) 
        => await _scoringService.UpdateScoreboardAsync(roomCode, scoreboard);
    public async Task SendFinalResultsAsync(string roomCode, object finalResults) 
        => await _scoringService.SendFinalResultsAsync(roomCode, finalResults);
    public async Task EndGameAsync(string roomCode, object finalResults) 
        => await _scoringService.EndGameAsync(roomCode, finalResults);
    public async Task SendScoreboardAsync(string roomCode, object scoreboard) 
        => await _scoringService.SendScoreboardAsync(roomCode, scoreboard);

    // IHostControlSocketService
    public async Task NotifyHostOnlyAsync(string roomCode, string message) 
        => await _hostControlService.NotifyHostOnlyAsync(roomCode, message);
    public async Task RequestNextQuestionAsync(string roomCode) 
        => await _hostControlService.RequestNextQuestionAsync(roomCode);
}