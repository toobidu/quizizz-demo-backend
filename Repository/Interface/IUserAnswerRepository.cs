using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Repository.Interface;

public interface IUserAnswerRepository
{
    Task<int> CreateUserAnswerAsync(UserAnswer userAnswer);
    Task<List<UserAnswer>> GetAnswersBySessionIdAsync(int sessionId);
    Task<List<UserAnswer>> GetAnswersByUserAndSessionAsync(int userId, int sessionId);
    Task<List<UserAnswer>> GetAnswersByUserIdAsync(int userId, int page, int limit);
    Task<List<UserAnswer>> GetByUserIdAsync(int userId);
    Task<int> GetTotalAnswersCountByUserIdAsync(int userId);
    Task<UserAnswer?> GetUserAnswerByIdAsync(int answerId);
    Task<bool> UpdateUserAnswerAsync(UserAnswer userAnswer);
    Task<bool> DeleteUserAnswerAsync(int answerId);
}