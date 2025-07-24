using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface IRoomRepository
{
    Task<Room?> GetRoomByIdAsync(int roomId);
    Task<Room?> GetRoomByCodeAsync(string roomCode);
    Task<List<Room>> GetRoomsByOwnerIdAsync(int ownerId);
    Task<int> CreateRoomAsync(Room room);
    Task<bool> UpdateRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(int roomId);
    
    // Các phương thức tương thích cũ
    Task<Room?> GetByIdAsync(int id);
    Task<Room?> GetByCodeAsync(string code);
    Task<int> AddAsync(Room room);
    Task<bool> UpdateAsync(Room room);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByCodeAsync(string code);
    Task<int> GetPlayerCountAsync(int roomId);
    Task<bool> UpdateStatusAsync(int roomId, string status);
    Task<bool> UpdateMaxPlayersAsync(int roomId, int maxPlayers);
    Task<string> GetRoomTopicNameAsync(int roomId);
    Task<int> GetRoomQuestionCountAsync(int roomId);
    Task<int> GetRoomCountdownTimeAsync(int roomId);
    Task<List<Room>> GetPublicWaitingRoomsAsync();
    Task<List<Room>> GetAllRoomsWithDetailsAsync();
}