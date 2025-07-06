using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IRoomPlayerRepository
{
    Task<RoomPlayer?> GetByUserIdAndRoomIdAsync(int userId, int roomId);
    Task<IEnumerable<RoomPlayer>> GetByRoomIdAsync(int roomId);
    Task<int> AddAsync(RoomPlayer roomPlayer);
    Task UpdateAsync(RoomPlayer roomPlayer);
    Task<bool> DeleteByUserIdAndRoomIdAsync(int userId, int roomId);
}
