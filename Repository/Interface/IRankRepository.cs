using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IRankRepository
{
    Task<Rank?> GetByIdAsync(int id);
    Task<Rank?> GetByUserIdAsync(int userId);
    Task<int> AddAsync(Rank rank);
    Task UpdateAsync(Rank rank);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Rank>> GetAllAsync();
}
