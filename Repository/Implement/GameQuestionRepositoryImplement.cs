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
    public async Task<IEnumerable<GameQuestion>> GetByGameSessionIdAsync(int gameSessionId)
    {
        const string sql = @"
            SELECT gq.*, q.*
            FROM game_questions gq
            JOIN questions q ON gq.question_id = q.id
            WHERE gq.game_session_id = @GameSessionId
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
            new { GameSessionId = gameSessionId },
            splitOn: "id"
        );
        return gameQuestions.Values;
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
        const string sql = @"
            INSERT INTO game_questions (game_session_id, question_id, question_order, time_limit)
            VALUES (@GameSessionId, @QuestionId, @QuestionOrder, @TimeLimit)";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, gameQuestion);
        return rowsAffected > 0;
    }
    public async Task<bool> CreateManyAsync(IEnumerable<GameQuestion> gameQuestions)
    {
        const string sql = @"
            INSERT INTO game_questions (game_session_id, question_id, question_order, time_limit)
            VALUES (@GameSessionId, @QuestionId, @QuestionOrder, @TimeLimit)";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, gameQuestions);
        return rowsAffected > 0;
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
