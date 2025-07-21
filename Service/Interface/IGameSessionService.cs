using ConsoleApp1.Model.DTO.Rooms.Games;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Service.Interface;

public interface IGameSessionService
{
    Task<GameSessionDTO> GetByIdAsync(int id);
    Task<GameSessionDTO> GetByRoomIdAsync(int roomId);
    Task<int> CreateAsync(GameSession gameSession);
    Task<bool> UpdateAsync(GameSession gameSession);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateGameStateAsync(int id, string gameState);
    Task<bool> UpdateCurrentQuestionIndexAsync(int id, int questionIndex);
    Task<bool> EndGameSessionAsync(int id);
    Task<IEnumerable<GameQuestionDTO>> GetGameQuestionsAsync(int gameSessionId);
    Task<bool> AddQuestionsToGameSessionAsync(int gameSessionId, IEnumerable<int> questionIds, int timeLimit);
}