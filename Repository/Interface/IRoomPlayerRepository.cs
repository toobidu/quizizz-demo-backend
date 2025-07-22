using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Repository.Interface;
public interface IRoomPlayerRepository
{
    Task<RoomPlayer?> GetByUserIdAndRoomIdAsync(int userId, int roomId);
    Task<IEnumerable<RoomPlayer>> GetByRoomIdAsync(int roomId);
    Task<Room?> GetActiveRoomByUserIdAsync(int userId);
    Task<int> AddAsync(RoomPlayer roomPlayer);
    Task UpdateAsync(RoomPlayer roomPlayer);
    Task<bool> DeleteByUserIdAndRoomIdAsync(int userId, int roomId);
    Task UpdateTimeAndScoreAsync(int roomId, int userId, TimeSpan timeTaken, int score);
    Task UpdatePlayerStatusAsync(int roomId, int userId, string status);
    Task UpdateSocketIdAsync(int roomId, int userId, string socketId);
    Task UpdateLastActivityAsync(int roomId, int userId);
    Task<IEnumerable<RoomPlayer>> GetBySocketIdAsync(string socketId);
}
