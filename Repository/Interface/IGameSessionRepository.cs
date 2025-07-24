using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Repository.Interface;

public interface IGameSessionRepository
{
    Task<int> CreateGameSessionAsync(GameSession gameSession);
    Task<GameSession?> GetGameSessionByIdAsync(int sessionId);
    Task<List<GameSession>> GetGameSessionsByRoomIdAsync(int roomId);
    Task<bool> UpdateGameSessionAsync(GameSession gameSession);
    Task<bool> DeleteGameSessionAsync(int sessionId);
    Task<List<LeaderboardDTO>> GetLeaderboardForSessionAsync(int sessionId);
    Task<object> GetGameStatsForSessionAsync(int sessionId);
    
    // Các phương thức tương thích cũ
    Task<GameSession?> GetByIdAsync(int id);
    Task<GameSession?> GetByRoomIdAsync(int roomId);
    Task<int> CreateAsync(GameSession gameSession);
    Task<bool> UpdateAsync(GameSession gameSession);
    Task<bool> DeleteAsync(int id);
    Task<bool> UpdateGameStateAsync(int sessionId, string gameState);
    Task<bool> UpdateCurrentQuestionIndexAsync(int sessionId, int index);
    Task<bool> EndGameSessionAsync(int sessionId);
}