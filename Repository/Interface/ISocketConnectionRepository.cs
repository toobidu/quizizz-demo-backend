using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Repository.Interface;
public interface ISocketConnectionRepository
{
    Task<SocketConnection> GetByIdAsync(int id);
    Task<SocketConnection> GetBySocketIdAsync(string socketId);
    Task<IEnumerable<SocketConnection>> GetByRoomIdAsync(int roomId);
    Task<IEnumerable<SocketConnection>> GetByUserIdAsync(int userId);
    Task<int> CreateAsync(SocketConnection socketConnection);
    Task<bool> UpdateAsync(SocketConnection socketConnection);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteBySocketIdAsync(string socketId);
    Task<bool> UpdateLastActivityAsync(string socketId);
}
