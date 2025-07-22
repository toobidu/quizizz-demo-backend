using ConsoleApp1.Model.DTO.Game;
namespace ConsoleApp1.Service.Interface.Socket;
public interface IRoomManagementSocketService
{
    Task JoinRoomAsync(string socketId, string roomCode, string username, int userId);
    Task LeaveRoomAsync(string socketId, string roomCode);
    Task LeaveRoomByUserIdAsync(int userId, string roomCode);
    Task UpdateRoomPlayersAsync(string roomCode);
    Task BroadcastPlayerJoinedEventAsync(string roomCode, int userId, string username);
    Task BroadcastPlayerLeftEventAsync(string roomCode, int userId, string username);
    Task BroadcastToAllConnectionsAsync(string roomCode, string eventName, object data);
    Task RequestPlayersUpdateAsync(string socketId, string roomCode);
    Task<GameRoom?> GetRoomAsync(string roomCode);
}
