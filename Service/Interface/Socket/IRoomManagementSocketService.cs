namespace ConsoleApp1.Service.Interface.Socket;

public interface IRoomManagementSocketService
{
    Task JoinRoomAsync(string socketId, string roomCode, string username, int userId);
    Task LeaveRoomAsync(string socketId, string roomCode);
    Task UpdateRoomPlayersAsync(string roomCode);
}