using ConsoleApp1.Data;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
namespace ConsoleApp1.Repository.Implement;
public class GameSessionRepositoryImplement : IGameSessionRepository
{
    private readonly DatabaseHelper _databaseHelper;
    public GameSessionRepositoryImplement(DatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }
    
    public async Task<int> CreateGameSessionAsync(GameSession gameSession)
    {
        const string sql = @"
            INSERT INTO game_sessions (room_id, game_state, current_question_index, start_time, end_time, time_limit)
            VALUES (@RoomId, @GameState, @CurrentQuestionIndex, @StartTime, @EndTime, @TimeLimit)
            RETURNING id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, gameSession);
    }
    
    public async Task<GameSession?> GetGameSessionByIdAsync(int sessionId)
    {
        const string sql = @"
            SELECT * FROM game_sessions
            WHERE id = @SessionId";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<GameSession>(sql, new { SessionId = sessionId });
    }
    
    public async Task<List<GameSession>> GetGameSessionsByRoomIdAsync(int roomId)
    {
        const string sql = @"
            SELECT * FROM game_sessions
            WHERE room_id = @RoomId
            ORDER BY created_at DESC";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<GameSession>(sql, new { RoomId = roomId });
        return result.ToList();
    }
    
    public async Task<bool> UpdateGameSessionAsync(GameSession gameSession)
    {
        const string sql = @"
            UPDATE game_sessions
            SET game_state = @GameState,
                current_question_index = @CurrentQuestionIndex,
                start_time = @StartTime,
                end_time = @EndTime,
                time_limit = @TimeLimit
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, gameSession);
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteGameSessionAsync(int sessionId)
    {
        const string sql = @"
            DELETE FROM game_sessions
            WHERE id = @SessionId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SessionId = sessionId });
        return rowsAffected > 0;
    }
    
    public async Task<List<LeaderboardDTO>> GetLeaderboardForSessionAsync(int sessionId)
    {
        const string sql = @"
            SELECT u.id as UserId, u.username as Username, u.full_name as FullName, 
                   SUM(ua.is_correct::int * 100) as Score, 
                   SUM(EXTRACT(EPOCH FROM ua.time_taken)) as TimeTaken
            FROM user_answers ua
            JOIN users u ON ua.user_id = u.id
            JOIN game_questions gq ON ua.question_id = gq.question_id
            WHERE gq.game_session_id = @SessionId
            GROUP BY u.id, u.username, u.full_name
            ORDER BY Score DESC, TimeTaken ASC";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<LeaderboardDTO>(sql, new { SessionId = sessionId });
        return result.ToList();
    }
    
    public async Task<object> GetGameStatsForSessionAsync(int sessionId)
    {
        const string sql = @"
            SELECT 
                COUNT(DISTINCT ua.user_id) as TotalPlayers,
                SUM(ua.is_correct::int) as TotalCorrectAnswers,
                COUNT(ua.id) as TotalAnswers,
                ROUND(AVG(EXTRACT(EPOCH FROM ua.time_taken)), 2) as AvgResponseTime
            FROM user_answers ua
            JOIN game_questions gq ON ua.question_id = gq.question_id
            WHERE gq.game_session_id = @SessionId";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync(sql, new { SessionId = sessionId });
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<GameSession?> GetByIdAsync(int id)
    {
        return await GetGameSessionByIdAsync(id);
    }
    
    public async Task<GameSession?> GetByRoomIdAsync(int roomId)
    {
        const string sql = @"
            SELECT * FROM game_sessions
            WHERE room_id = @RoomId
            ORDER BY created_at DESC
            LIMIT 1";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<GameSession>(sql, new { RoomId = roomId });
    }
    
    public async Task<int> CreateAsync(GameSession gameSession)
    {
        return await CreateGameSessionAsync(gameSession);
    }
    
    public async Task<bool> UpdateAsync(GameSession gameSession)
    {
        return await UpdateGameSessionAsync(gameSession);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        return await DeleteGameSessionAsync(id);
    }
    
    public async Task<bool> UpdateGameStateAsync(int id, string gameState)
    {
        const string sql = @"
            UPDATE game_sessions
            SET game_state = @GameState
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, GameState = gameState });
        return rowsAffected > 0;
    }
    
    public async Task<bool> UpdateCurrentQuestionIndexAsync(int id, int questionIndex)
    {
        const string sql = @"
            UPDATE game_sessions
            SET current_question_index = @QuestionIndex
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, QuestionIndex = questionIndex });
        return rowsAffected > 0;
    }
    
    public async Task<bool> EndGameSessionAsync(int id)
    {
        const string sql = @"
            UPDATE game_sessions
            SET end_time = CURRENT_TIMESTAMP,
                game_state = 'completed'
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}
