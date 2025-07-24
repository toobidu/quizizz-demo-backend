using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Repository.Interface;

public interface IRankRepository
{
    Task<List<Rank>> GetGlobalRanksAsync(int page, int limit, string level);
    Task<int> GetTotalRanksCountAsync(string level);
    Task<List<Rank>> GetTopRanksAsync(int top);
    Task<Rank?> GetRankByUserIdAsync(int userId);
    Task<int> GetUserGlobalRankPositionAsync(int userId);
    Task<string> CalculateUserLevelAsync(int totalScore);
    Task<bool> UpdateRankAsync(Rank rank);
    Task<int> CreateRankAsync(Rank rank);
    Task<List<object>> GetUserRankHistoryAsync(int userId, int page, int limit);
    Task<int> GetUserRankHistoryCountAsync(int userId);
    Task<double> GetAverageScoreAsync();
    Task<int> GetHighestScoreAsync();
    Task<Dictionary<string, int>> GetLevelDistributionAsync();
    Task<bool> UpdateUserLevelAsync(int userId, string newLevel, int experiencePoints);
}