using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
using System.Data;
namespace ConsoleApp1.Repository.Implement;
public class GameQuestionRepositoryImplement : IGameQuestionRepository
{
    private readonly DatabaseHelper _databaseHelper;
    public GameQuestionRepositoryImplement(DatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }
    
    public async Task<int> AddQuestionToSessionAsync(GameQuestion gameQuestion)
    {
        const string sql = @"
            INSERT INTO game_questions (game_session_id, question_id, question_order, time_limit)
            VALUES (@GameSessionId, @QuestionId, @QuestionOrder, @TimeLimit)
            RETURNING game_session_id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { 
            GameSessionId = gameQuestion.GameSessionId, 
            QuestionId = gameQuestion.QuestionId, 
            QuestionOrder = gameQuestion.QuestionOrder, 
            TimeLimit = gameQuestion.TimeLimit 
        });
    }
    
    public async Task<List<GameQuestion>> GetQuestionsForSessionAsync(int sessionId)
    {
        const string sql = @"
            SELECT gq.*, q.*
            FROM game_questions gq
            JOIN questions q ON gq.question_id = q.id
            WHERE gq.game_session_id = @SessionId
            ORDER BY gq.question_order";
        using var connection = _databaseHelper.GetConnection();
        var gameQuestions = new Dictionary<int, GameQuestion>();
        await connection.QueryAsync<GameQuestion, Model.Entity.Questions.Question, GameQuestion>(
            sql,
            (gameQuestion, question) =>
            {
                if (!gameQuestions.TryGetValue(gameQuestion.QuestionId, out var existingGameQuestion))
                {
                    existingGameQuestion = gameQuestion;
                    existingGameQuestion.Question = question;
                    gameQuestions.Add(gameQuestion.QuestionId, existingGameQuestion);
                }
                return existingGameQuestion;
            },
            new { SessionId = sessionId },
            splitOn: "id"
        );
        return gameQuestions.Values.ToList();
    }
    
    public async Task<GameQuestion?> GetQuestionByOrderAsync(int sessionId, int questionOrder)
    {
        const string sql = @"
            SELECT gq.*, q.*
            FROM game_questions gq
            JOIN questions q ON gq.question_id = q.id
            WHERE gq.game_session_id = @SessionId AND gq.question_order = @QuestionOrder";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<GameQuestion, Model.Entity.Questions.Question, GameQuestion>(
            sql,
            (gameQuestion, question) =>
            {
                gameQuestion.Question = question;
                return gameQuestion;
            },
            new { SessionId = sessionId, QuestionOrder = questionOrder },
            splitOn: "id"
        );
        return result.FirstOrDefault();
    }
    
    public async Task<int> GetQuestionCountForSessionAsync(int sessionId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM game_questions
            WHERE game_session_id = @SessionId";
        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { SessionId = sessionId });
    }
    
    public async Task<bool> UpdateGameQuestionAsync(GameQuestion gameQuestion)
    {
        const string sql = @"
            UPDATE game_questions
            SET question_order = @QuestionOrder,
                time_limit = @TimeLimit
            WHERE game_session_id = @GameSessionId AND question_id = @QuestionId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { 
            GameSessionId = gameQuestion.GameSessionId, 
            QuestionId = gameQuestion.QuestionId, 
            QuestionOrder = gameQuestion.QuestionOrder, 
            TimeLimit = gameQuestion.TimeLimit 
        });
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteGameQuestionAsync(int sessionId, int questionId)
    {
        const string sql = @"
            DELETE FROM game_questions
            WHERE game_session_id = @SessionId AND question_id = @QuestionId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SessionId = sessionId, QuestionId = questionId });
        return rowsAffected > 0;
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<List<GameQuestion>> GetByGameSessionIdAsync(int gameSessionId)
    {
        return await GetQuestionsForSessionAsync(gameSessionId);
    }
    
    public async Task<GameQuestion> GetByGameSessionAndQuestionIdAsync(int gameSessionId, int questionId)
    {
        const string sql = @"
            SELECT gq.*, q.*
            FROM game_questions gq
            JOIN questions q ON gq.question_id = q.id
            WHERE gq.game_session_id = @GameSessionId AND gq.question_id = @QuestionId";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<GameQuestion, Model.Entity.Questions.Question, GameQuestion>(
            sql,
            (gameQuestion, question) =>
            {
                gameQuestion.Question = question;
                return gameQuestion;
            },
            new { GameSessionId = gameSessionId, QuestionId = questionId },
            splitOn: "id"
        );
        return result.FirstOrDefault();
    }
    
    public async Task<bool> CreateAsync(GameQuestion gameQuestion)
    {
        await AddQuestionToSessionAsync(gameQuestion);
        return true;
    }
    
    public async Task<int> CreateManyAsync(List<GameQuestion> gameQuestions)
    {
        const string sql = @"
            INSERT INTO game_questions (game_session_id, question_id, question_order, time_limit)
            VALUES (@GameSessionId, @QuestionId, @QuestionOrder, @TimeLimit)";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, gameQuestions);
        return rowsAffected;
    }
    
    public async Task<bool> DeleteByGameSessionIdAsync(int gameSessionId)
    {
        const string sql = @"
            DELETE FROM game_questions
            WHERE game_session_id = @GameSessionId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { GameSessionId = gameSessionId });
        return rowsAffected > 0;
    }
}
