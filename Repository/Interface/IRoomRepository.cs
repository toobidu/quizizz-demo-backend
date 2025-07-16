using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);
    Task<Room?> GetByCodeAsync(string code);
    Task<int> AddAsync(Room room);
    Task UpdateAsync(Room room);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Room>> GetAllAsync();
    Task<IEnumerable<Room>> GetPublicWaitingRoomsAsync();
    Task<IEnumerable<Room>> GetAllRoomsWithDetailsAsync();
    Task<Room> UpdateStatusAsync(int roomId, string status);
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<int> GetPlayerCountAsync(int roomId);
    Task<string?> GetRoomTopicNameAsync(int roomId);
    Task<int> GetRoomQuestionCountAsync(int roomId);
    Task<int> GetRoomCountdownTimeAsync(int roomId);
    Task UpdateMaxPlayersAsync(int roomId, int maxPlayers);
    Task<bool> ExistsByCodeAsync(string roomCode);
}
