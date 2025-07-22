namespace ConsoleApp1.Service.Interface.Socket;
public interface IPlayerInteractionSocketService
{
    Task ReceiveAnswerAsync(string roomCode, string username, object answer, long timestamp);
    Task UpdatePlayerStatusAsync(string roomCode, string username, string status);
}
