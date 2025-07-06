using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int id);
    Task<Room?> GetByCodeAsync(string code);
    Task<int> AddAsync(Room room);
    Task UpdateAsync(Room room);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Room>> GetAllAsync();
}
