using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class UserAnswerRepositoryImplement : IUserAnswerRepository
{
    private readonly DatabaseHelper _dbHelper;
    
    public UserAnswerRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    
    public async Task<List<UserAnswer>> GetByUserIdAsync(int userId)
    {
        const string sql = @"
            SELECT * FROM user_answers
            WHERE user_id = @UserId
            ORDER BY created_at DESC";
        
        using var connection = CreateConnection();
        var answers = await connection.QueryAsync<UserAnswer>(sql, new { UserId = userId });
        return answers.ToList();
    }

    public async Task<int> CreateUserAnswerAsync(UserAnswer userAnswer)
    {
        const string sql = @"
            INSERT INTO user_answers (user_id, room_id, question_id, answer_id, is_correct, time_taken, game_session_id, score)
            VALUES (@UserId, @RoomId, @QuestionId, @AnswerId, @IsCorrect, @TimeTaken, @GameSessionId, @Score)
            RETURNING user_id";
        
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, userAnswer);
    }

    public async Task<List<UserAnswer>> GetAnswersBySessionIdAsync(int sessionId)
    {
        const string sql = @"
            SELECT * FROM user_answers
            WHERE game_session_id = @SessionId
            ORDER BY created_at";
        
        using var connection = CreateConnection();
        var answers = await connection.QueryAsync<UserAnswer>(sql, new { SessionId = sessionId });
        return answers.ToList();
    }

    public async Task<List<UserAnswer>> GetAnswersByUserAndSessionAsync(int userId, int sessionId)
    {
        const string sql = @"
            SELECT * FROM user_answers
            WHERE user_id = @UserId AND game_session_id = @SessionId
            ORDER BY created_at";
        
        using var connection = CreateConnection();
        var answers = await connection.QueryAsync<UserAnswer>(sql, new { UserId = userId, SessionId = sessionId });
        return answers.ToList();
    }

    public async Task<List<UserAnswer>> GetAnswersByUserIdAsync(int userId, int page, int limit)
    {
        int offset = (page - 1) * limit;
        const string sql = @"
            SELECT * FROM user_answers
            WHERE user_id = @UserId
            ORDER BY created_at DESC
            LIMIT @Limit OFFSET @Offset";
        
        using var connection = CreateConnection();
        var answers = await connection.QueryAsync<UserAnswer>(sql, new { UserId = userId, Limit = limit, Offset = offset });
        return answers.ToList();
    }

    public async Task<int> GetTotalAnswersCountByUserIdAsync(int userId)
    {
        const string sql = "SELECT COUNT(*) FROM user_answers WHERE user_id = @UserId";
        using var connection = CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task<UserAnswer?> GetUserAnswerByIdAsync(int answerId)
    {
        const string sql = @"
            SELECT * FROM user_answers
            WHERE id = @AnswerId";
        
        using var connection = CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<UserAnswer>(sql, new { AnswerId = answerId });
    }

    public async Task<bool> UpdateUserAnswerAsync(UserAnswer userAnswer)
    {
        const string sql = @"
            UPDATE user_answers
            SET is_correct = @IsCorrect,
                score = @Score,
                updated_at = CURRENT_TIMESTAMP
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        
        using var connection = CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, userAnswer);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteUserAnswerAsync(int answerId)
    {
        const string sql = "DELETE FROM user_answers WHERE id = @AnswerId";
        using var connection = CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { AnswerId = answerId });
        return rowsAffected > 0;
    }
}