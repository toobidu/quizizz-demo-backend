using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class AnswerRepositoryImplement : IAnswerRepository
{
    private readonly DatabaseHelper _dbHelper;
    public AnswerRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    public async Task<Answer?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM answers WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Answer>(query, new { Id = id });
    }
    public async Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId)
    {
        const string query = @"SELECT * FROM answers WHERE question_id = @QuestionId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Answer>(query, new { QuestionId = questionId });
        return result.ToList();
    }
    public async Task<int> AddAsync(Answer answer)
    {
        const string query = @"
            INSERT INTO answers (question_id, answer_text, is_correct) 
            VALUES (@QuestionId, @AnswerText, @IsCorrect) 
            RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, answer);
    }
    public async Task UpdateAsync(Answer answer)
    {
        const string query = @"
            UPDATE answers 
            SET answer_text = @AnswerText, is_correct = @IsCorrect 
            WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, answer);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM answers WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }
    public async Task<bool> DeleteByQuestionIdAsync(int questionId)
    {
        const string query = @"DELETE FROM answers WHERE question_id = @QuestionId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { QuestionId = questionId });
        return affected > 0;
    }
}
