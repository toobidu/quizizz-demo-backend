namespace ConsoleApp1.Service.Interface.Socket;

public interface IScoringSocketService
{
    Task UpdateScoreboardAsync(string roomCode, object scoreboard);
    Task SendFinalResultsAsync(string roomCode, object finalResults);
    Task EndGameAsync(string roomCode, object finalResults);
    Task SendScoreboardAsync(string roomCode, object scoreboard);
}