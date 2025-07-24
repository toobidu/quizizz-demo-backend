using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface ISocketConnectionRepository
{
    Task<int> CreateConnectionAsync(SocketConnection connection);
    Task<SocketConnection?> GetConnectionByIdAsync(int id);
    Task<SocketConnection?> GetConnectionBySocketIdAsync(string socketId);
    Task<List<SocketConnection>> GetConnectionsByRoomIdAsync(int roomId);
    Task<List<SocketConnection>> GetConnectionsByUserIdAsync(int userId);
    Task<bool> UpdateConnectionAsync(SocketConnection connection);
    Task<bool> DeleteConnectionAsync(string socketId);
    Task<bool> DeleteConnectionByIdAsync(int id);
    Task<bool> UpdateLastActivityAsync(string socketId);
    
    // Các phương thức tương thích cũ
    Task<SocketConnection?> GetByIdAsync(int id);
    Task<SocketConnection?> GetBySocketIdAsync(string socketId);
    Task<List<SocketConnection>> GetByRoomIdAsync(int roomId);
    Task<List<SocketConnection>> GetByUserIdAsync(int userId);
    Task<int> CreateAsync(SocketConnection connection);
    Task<bool> UpdateAsync(SocketConnection connection);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteBySocketIdAsync(string socketId);
}