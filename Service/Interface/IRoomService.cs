using ConsoleApp1.Model.DTO.Rooms;

namespace ConsoleApp1.Service.Interface;

public interface IRoomService
{
    Task<RoomDTO> JoinRoomAsync(JoinRoomRequest request);
    Task<RoomDTO> CreateRoomAsync(CreateRoomRequest request);
    Task<RoomDTO> GetRoomByIdAsync(int roomId);
    Task<List<RoomDTO>> GetAllRoomsAsync();
    Task<bool> LeaveRoomAsync(int roomId, int userId);
    Task<bool> DeleteRoomAsync(int roomId);
    Task<bool> UpdateRoomAsync(int roomId, RoomDTO request);
    Task<bool> IsUserInRoomAsync(int roomId, int userId);
    Task<bool> IsRoomCodeExistsAsync(string roomCode);
    Task<bool> IsRoomNameExistsAsync(string roomName);
    Task<bool> IsRoomOwnerAsync(int roomId, int userId);
    Task<bool> IsRoomPrivateAsync(int roomId);
    Task<bool> IsRoomFullAsync(int roomId);
}