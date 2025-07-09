using System.Data;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class UserAnswerRepositoryImplement : IUserAnswerRepository
{
    public readonly string ConnectionString;

    public UserAnswerRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<UserAnswer?> GetByUserIdRoomIdQuestionIdAsync(int userId, int roomId, int questionId)
    {
        const string query = @"
            SELECT * FROM user_answers 
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<UserAnswer>(query, new { UserId = userId, RoomId = roomId, QuestionId = questionId });
    }

    public async Task<IEnumerable<UserAnswer>> GetByUserIdAsync(int userId)
    {
        const string query = @"SELECT * FROM user_answers WHERE user_id = @UserId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<UserAnswer>(query, new { UserId = userId });
        return result.ToList();
    }

    public async Task<IEnumerable<UserAnswer>> GetByRoomIdAsync(int roomId)
    {
        const string query = @"SELECT * FROM user_answers WHERE room_id = @RoomId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<UserAnswer>(query, new { RoomId = roomId });
        return result.ToList();
    }

    public async Task<IEnumerable<UserAnswer>> GetByQuestionIdAsync(int questionId)
    {
        const string query = @"SELECT * FROM user_answers WHERE question_id = @QuestionId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<UserAnswer>(query, new { QuestionId = questionId });
        return result.ToList();
    }

    public async Task<int> AddAsync(UserAnswer answer)
    {
        const string query = @"
            INSERT INTO user_answers (user_id, room_id, question_id, answer_id, is_correct, time_taken) 
            VALUES (@UserId, @RoomId, @QuestionId, @AnswerId, @IsCorrect, @TimeTaken) 
            RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, answer);
    }

    public async Task UpdateAsync(UserAnswer answer)
    {
        const string query = @"
            UPDATE user_answers 
            SET answer_id = @AnswerId, is_correct = @IsCorrect, time_taken = @TimeTaken 
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, answer);
    }

    public async Task<bool> DeleteByUserIdRoomIdQuestionIdAsync(int userId, int roomId, int questionId)
    {
        const string query = @"
            DELETE FROM user_answers 
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { UserId = userId, RoomId = roomId, QuestionId = questionId });
        return affected > 0;
    }
    
    public async Task<IEnumerable<UserAnswer>> GetByRoomIdAndQuestionIdAsync(int roomId, int questionId)
    {
        const string query = @"SELECT * FROM user_answers 
            WHERE room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserAnswer>(query, 
            new { RoomId = roomId, QuestionId = questionId });
    }

    public async Task<Dictionary<int, int>> GetAnswerDistributionAsync(int questionId)
    {
        const string query = @"
            SELECT answer_id, COUNT(*) as count 
            FROM user_answers 
            WHERE question_id = @QuestionId 
            GROUP BY answer_id";
        using var conn = CreateConnection();
        var results = await conn.QueryAsync<(int AnswerId, int Count)>(query, 
            new { QuestionId = questionId });
        return results.ToDictionary(x => x.AnswerId, x => x.Count);
    }

    public async Task<double> GetAverageResponseTimeAsync(int questionId)
    {
        const string query = @"
            SELECT AVG(EXTRACT(EPOCH FROM time_taken)) 
            FROM user_answers 
            WHERE question_id = @QuestionId";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<double>(query, new { QuestionId = questionId });
    }
    
    public async Task<IEnumerable<(int QuestionId, double AverageTime)>> GetAverageTimesByRoomAsync(int roomId)
    {
        const string query = @"
        SELECT question_id, 
               AVG(EXTRACT(EPOCH FROM time_taken)) as avg_time
        FROM user_answers
        WHERE room_id = @RoomId
        GROUP BY question_id";
        using var conn = CreateConnection();
        return await conn.QueryAsync<(int QuestionId, double AverageTime)>(query, new { RoomId = roomId });
    }

}
