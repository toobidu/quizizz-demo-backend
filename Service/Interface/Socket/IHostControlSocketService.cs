namespace ConsoleApp1.Service.Interface.Socket;

public interface IHostControlSocketService
{
    Task NotifyHostOnlyAsync(string roomCode, string message);
    Task RequestNextQuestionAsync(string roomCode);
}