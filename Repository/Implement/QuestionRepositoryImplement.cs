using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class QuestionRepositoryImplement : IQuestionRepository
{
    public readonly string ConnectionString;

    public QuestionRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<Question?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM questions WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Question>(query, new { Id = id });
    }

    public async Task<IEnumerable<Question>> GetAllAsync()
    {
        const string query = @"SELECT * FROM questions";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Question>(query);
        return result.ToList();
    }

    public async Task<int> AddAsync(Question question)
    {
        const string query = @"INSERT INTO questions (question_text) 
                               VALUES (@QuestionText) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, question);
    }

    public async Task UpdateAsync(Question question)
    {
        const string query = @"UPDATE questions 
                               SET question_text = @QuestionText 
                               WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, question);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM questions WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }
}