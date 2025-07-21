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

    public async Task<IEnumerable<UserAnswer>> GetRecentAnswersByUserIdAsync(int userId, int gameLimit)
    {
        const string query = @"
            SELECT ua.*, q.topic_id 
            FROM user_answers ua
            JOIN questions q ON ua.question_id = q.id
            WHERE ua.user_id = @UserId 
            ORDER BY ua.created_at DESC 
            LIMIT @GameLimit * 10";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<UserAnswer, int, UserAnswer>(query,
            (userAnswer, topicId) => {
                userAnswer.Question = new() { TopicId = topicId };
                return userAnswer;
            },
            new { UserId = userId, GameLimit = gameLimit },
            splitOn: "topic_id");
        return result.ToList();
    }

    public async Task<int> AddAsync(UserAnswer answer)
    {
        const string query = @"
            INSERT INTO user_answers (user_id, room_id, question_id, answer_id, is_correct, time_taken, game_session_id, score) 
            VALUES (@UserId, @RoomId, @QuestionId, @AnswerId, @IsCorrect, @TimeTaken, @GameSessionId, @Score) 
            RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, answer);
    }

    public async Task UpdateAsync(UserAnswer answer)
    {
        const string query = @"
            UPDATE user_answers 
            SET answer_id = @AnswerId, is_correct = @IsCorrect, time_taken = @TimeTaken,
                game_session_id = @GameSessionId, score = @Score 
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
    
    public async Task<IEnumerable<UserAnswer>> GetByGameSessionIdAsync(int gameSessionId)
    {
        const string query = @"SELECT * FROM user_answers WHERE game_session_id = @GameSessionId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<UserAnswer>(query, new { GameSessionId = gameSessionId });
        return result.ToList();
    }
    
    public async Task<IEnumerable<UserAnswer>> GetByGameSessionIdAndQuestionIdAsync(int gameSessionId, int questionId)
    {
        const string query = @"SELECT * FROM user_answers 
            WHERE game_session_id = @GameSessionId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<UserAnswer>(query, 
            new { GameSessionId = gameSessionId, QuestionId = questionId });
    }
    
    public async Task UpdateScoreAsync(int userId, int roomId, int questionId, int score)
    {
        const string query = @"
            UPDATE user_answers 
            SET score = @Score 
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { UserId = userId, RoomId = roomId, QuestionId = questionId, Score = score });
    }
    
    public async Task UpdateGameSessionIdAsync(int userId, int roomId, int questionId, int gameSessionId)
    {
        const string query = @"
            UPDATE user_answers 
            SET game_session_id = @GameSessionId 
            WHERE user_id = @UserId AND room_id = @RoomId AND question_id = @QuestionId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { UserId = userId, RoomId = roomId, QuestionId = questionId, GameSessionId = gameSessionId });
    }
}
