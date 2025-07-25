namespace ConsoleApp1.Service.Interface;

public interface ISocketConnectionService
{
    // WebSocket server management methods
    Task StartAsync(int port);
    Task StopAsync();
    
    // Broadcasting methods
    Task BroadcastToRoomAsync(string roomCode, string eventType, object data);
    Task BroadcastToUserAsync(int userId, string eventType, object data);
    Task BroadcastToSocketAsync(string socketId, string eventType, object data);
    Task BroadcastToAllAsync(string eventType, object data);
}