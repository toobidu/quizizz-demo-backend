namespace ConsoleApp1.Service.Interface;

public interface ISocketService
{
    Task StartAsync(int port);
    Task StopAsync();
    
    // Room Management
    Task JoinRoomAsync(string socketId, string roomCode, string username, int userId);
    Task LeaveRoomAsync(string socketId, string roomCode);
    Task UpdateRoomPlayersAsync(string roomCode);
    
    // Game Flow
    Task StartGameAsync(string roomCode);
    Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit);
    Task SendNextQuestionToPlayerAsync(string roomCode, string username);
    Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions);
    Task SendGameTimerUpdateAsync(string roomCode);
    Task GetPlayerProgressAsync(string roomCode, string username);
    Task BroadcastPlayerProgressAsync(string roomCode);
    Task CleanupGameSessionAsync(string roomCode);
    
    // Player Interaction
    Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp);
    Task UpdatePlayerStatusAsync(string roomCode, string username, string status);
    
    // Scoring & Leaderboard  
    Task UpdateScoreboardAsync(string roomCode, object scoreboard);
    Task SendFinalResultsAsync(string roomCode, object finalResults);
    Task EndGameAsync(string roomCode, object finalResults);
    Task SendScoreboardAsync(string roomCode, object scoreboard);
    
    // Game State
    Task UpdateGameStateAsync(string roomCode, string gameState);
    Task SendCountdownAsync(string roomCode, int countdown);
    
    // Host Controls
    Task NotifyHostOnlyAsync(string roomCode, string message);
    Task RequestNextQuestionAsync(string roomCode);
}