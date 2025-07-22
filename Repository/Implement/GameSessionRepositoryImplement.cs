using ConsoleApp1.Data;
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
    public async Task<GameSession> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT * FROM game_sessions
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<GameSession>(sql, new { Id = id });
    }
    public async Task<GameSession> GetByRoomIdAsync(int roomId)
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
        const string sql = @"
            INSERT INTO game_sessions (room_id, game_state, current_question_index, start_time, end_time, time_limit)
            VALUES (@RoomId, @GameState, @CurrentQuestionIndex, @StartTime, @EndTime, @TimeLimit)
            RETURNING id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, gameSession);
    }
    public async Task<bool> UpdateAsync(GameSession gameSession)
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
    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
            DELETE FROM game_sessions
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
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
    public async Task<bool> EndGameSessionAsync(int id, DateTime endTime)
    {
        const string sql = @"
            UPDATE game_sessions
            SET end_time = @EndTime,
                game_state = 'completed'
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, EndTime = endTime });
        return rowsAffected > 0;
    }
}
