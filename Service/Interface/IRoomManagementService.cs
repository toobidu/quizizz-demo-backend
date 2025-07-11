namespace ConsoleApp1.Service.Interface;

public interface IRoomManagementService
{
    Task<bool> LeaveRoomAsync(int userId, int roomId);
    Task<bool> TransferHostAsync(int roomId, int currentHostId, int newHostId);
    Task<bool> DeleteRoomIfEmptyAsync(int roomId);
}