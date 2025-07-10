using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class RoomServiceImplement : IRoomService
{
    public Task<RoomDTO> JoinRoomAsync(JoinRoomRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<RoomDTO> CreateRoomAsync(CreateRoomRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<RoomDTO> GetRoomByIdAsync(int roomId)
    {
        throw new NotImplementedException();
    }

    public Task<List<RoomDTO>> GetAllRoomsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<bool> LeaveRoomAsync(int roomId, int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteRoomAsync(int roomId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> UpdateRoomAsync(int roomId, RoomDTO request)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsUserInRoomAsync(int roomId, int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRoomCodeExistsAsync(string roomCode)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRoomNameExistsAsync(string roomName)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRoomOwnerAsync(int roomId, int userId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRoomPrivateAsync(int roomId)
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsRoomFullAsync(int roomId)
    {
        throw new NotImplementedException();
    }
}