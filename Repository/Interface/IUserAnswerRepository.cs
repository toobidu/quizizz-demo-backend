using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Repository.Interface;

public interface IUserAnswerRepository
{
    Task<UserAnswer?> GetByUserIdRoomIdQuestionIdAsync(int userId, int roomId, int questionId);
    Task<IEnumerable<UserAnswer>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserAnswer>> GetByRoomIdAsync(int roomId);
    Task<IEnumerable<UserAnswer>> GetByQuestionIdAsync(int questionId);
    Task<int> AddAsync(UserAnswer answer);
    Task UpdateAsync(UserAnswer answer);
    Task<bool> DeleteByUserIdRoomIdQuestionIdAsync(int userId, int roomId, int questionId);
    Task<IEnumerable<UserAnswer>> GetByRoomIdAndQuestionIdAsync(int roomId, int questionId);
    Task<Dictionary<int, int>> GetAnswerDistributionAsync(int questionId);
    Task<double> GetAverageResponseTimeAsync(int questionId);
    Task<IEnumerable<(int QuestionId, double AverageTime)>> GetAverageTimesByRoomAsync(int roomId);
}
