namespace ConsoleApp1.Service.Interface.Socket;

public interface IGameFlowSocketService
{
    Task StartGameAsync(string roomCode);
    Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit);
    Task SendNextQuestionToPlayerAsync(string roomCode, string username);
    Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions);
    Task SendGameTimerUpdateAsync(string roomCode);
    Task GetPlayerProgressAsync(string roomCode, string username);
    Task BroadcastPlayerProgressAsync(string roomCode);
    Task CleanupGameSessionAsync(string roomCode);
    Task UpdateGameStateAsync(string roomCode, string gameState);
    Task SendCountdownAsync(string roomCode, int countdown);
}