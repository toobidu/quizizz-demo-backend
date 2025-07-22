namespace ConsoleApp1.Service.Interface.Socket;
public interface ISocketConnectionService
{
    Task StartAsync(int port);
    Task StopAsync();
}
