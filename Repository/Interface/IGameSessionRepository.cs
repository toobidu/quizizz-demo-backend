using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Repository.Interface;
public interface IGameSessionRepository
{
    Task<GameSession> GetByIdAsync(int id);
    Task<GameSession> GetByRoomIdAsync(int roomId);
    Task<int> CreateAsync(GameSession gameSession);
    Task<bool> UpdateAsync(GameSession gameSession);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateGameStateAsync(int id, string gameState);
    Task<bool> UpdateCurrentQuestionIndexAsync(int id, int questionIndex);
    Task<bool> EndGameSessionAsync(int id, DateTime endTime);
}
