using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Repository.Interface;
public interface IGameQuestionRepository
{
    Task<IEnumerable<GameQuestion>> GetByGameSessionIdAsync(int gameSessionId);
    Task<GameQuestion> GetByGameSessionAndQuestionIdAsync(int gameSessionId, int questionId);
    Task<bool> CreateAsync(GameQuestion gameQuestion);
    Task<bool> CreateManyAsync(IEnumerable<GameQuestion> gameQuestions);
    Task<bool> DeleteByGameSessionIdAsync(int gameSessionId);
}
