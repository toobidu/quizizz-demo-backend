using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Repository.Interface;

public interface IRankRepository
{
    Task<Rank?> GetByIdAsync(int id);
    Task<Rank?> GetByUserIdAsync(int userId);
    Task<int> AddAsync(Rank rank);
    Task UpdateAsync(Rank rank);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Rank>> GetAllAsync();
    Task<IEnumerable<Rank>> GetTopPlayersAsync(int limit);
    Task<(int TotalGames, double AverageScore)> GetUserStatsAsync(int userId);
}