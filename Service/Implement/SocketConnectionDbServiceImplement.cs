using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class SocketConnectionDbServiceImplement : ISocketConnectionDbService
{
    private readonly ISocketConnectionRepository _socketConnectionRepository;

    public SocketConnectionDbServiceImplement(ISocketConnectionRepository socketConnectionRepository)
    {
        _socketConnectionRepository = socketConnectionRepository;
    }

    public async Task<SocketConnectionDTO> GetByIdAsync(int id)
    {
        var socketConnection = await _socketConnectionRepository.GetByIdAsync(id);
        if (socketConnection == null)
            return null;

        return MapToDTO(socketConnection);
    }

    public async Task<SocketConnectionDTO> GetBySocketIdAsync(string socketId)
    {
        var socketConnection = await _socketConnectionRepository.GetBySocketIdAsync(socketId);
        if (socketConnection == null)
            return null;

        return MapToDTO(socketConnection);
    }

    public async Task<IEnumerable<SocketConnectionDTO>> GetByRoomIdAsync(int roomId)
    {
        var socketConnections = await _socketConnectionRepository.GetByRoomIdAsync(roomId);
        return socketConnections.Select(MapToDTO);
    }

    public async Task<IEnumerable<SocketConnectionDTO>> GetByUserIdAsync(int userId)
    {
        var socketConnections = await _socketConnectionRepository.GetByUserIdAsync(userId);
        return socketConnections.Select(MapToDTO);
    }

    public async Task<int> CreateAsync(SocketConnection socketConnection)
    {
        return await _socketConnectionRepository.CreateAsync(socketConnection);
    }

    public async Task<bool> UpdateAsync(SocketConnection socketConnection)
    {
        return await _socketConnectionRepository.UpdateAsync(socketConnection);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await _socketConnectionRepository.DeleteAsync(id);
    }

    public async Task<bool> DeleteBySocketIdAsync(string socketId)
    {
        return await _socketConnectionRepository.DeleteBySocketIdAsync(socketId);
    }

    public async Task<bool> UpdateLastActivityAsync(string socketId)
    {
        return await _socketConnectionRepository.UpdateLastActivityAsync(socketId);
    }

    private SocketConnectionDTO MapToDTO(SocketConnection socketConnection)
    {
        return new SocketConnectionDTO
        {
            Id = socketConnection.Id,
            SocketId = socketConnection.SocketId,
            UserId = socketConnection.UserId,
            RoomId = socketConnection.RoomId,
            ConnectedAt = socketConnection.ConnectedAt,
            LastActivity = socketConnection.LastActivity
        };
    }
}