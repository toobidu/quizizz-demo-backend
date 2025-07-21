using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Service.Interface;

public interface ISocketConnectionService
{
    Task<SocketConnectionDTO> GetByIdAsync(int id);
    Task<SocketConnectionDTO> GetBySocketIdAsync(string socketId);
    Task<IEnumerable<SocketConnectionDTO>> GetByRoomIdAsync(int roomId);
    Task<IEnumerable<SocketConnectionDTO>> GetByUserIdAsync(int userId);
    Task<int> CreateAsync(SocketConnection socketConnection);
    Task<bool> UpdateAsync(SocketConnection socketConnection);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteBySocketIdAsync(string socketId);
    Task<bool> UpdateLastActivityAsync(string socketId);
}