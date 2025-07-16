using ConsoleApp1.Model.DTO.Rooms;

namespace ConsoleApp1.Service.Interface;

public interface IJoinRoomService
{
    Task<RoomDTO?> JoinPublicRoomAsync(int roomId, int playerId);
    Task<RoomDTO?> JoinPrivateRoomAsync(string roomCode, int playerId);
    Task<bool> LeaveRoomAsync(int roomId, int playerId);
    Task<bool> LeaveRoomByCodeAsync(string roomCode, int playerId);
    Task<RoomDTO?> GetRoomByCodeAsync(string roomCode);
    Task<IEnumerable<RoomSummaryDTO>> GetPublicRoomsAsync();
    Task<IEnumerable<RoomSummaryDTO>> GetAllRoomsAsync();
    Task<IEnumerable<PlayerInRoomDTO>> GetPlayersInRoomAsync(int roomId);
    Task<RoomDetailsDTO?> GetRoomDetailsAsync(int roomId);
    Task<bool> StartGameAsync(int roomId, int userId);
}