using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface IGameQuestionRepository
{
    Task<int> AddQuestionToSessionAsync(GameQuestion gameQuestion);
    Task<List<GameQuestion>> GetQuestionsForSessionAsync(int sessionId);
    Task<GameQuestion?> GetQuestionByOrderAsync(int sessionId, int questionOrder);
    Task<int> GetQuestionCountForSessionAsync(int sessionId);
    Task<bool> UpdateGameQuestionAsync(GameQuestion gameQuestion);
    Task<bool> DeleteGameQuestionAsync(int sessionId, int questionId);
    
    // Các phương thức tương thích cũ
    Task<List<GameQuestion>> GetByGameSessionIdAsync(int sessionId);
    Task<int> CreateManyAsync(List<GameQuestion> gameQuestions);
}