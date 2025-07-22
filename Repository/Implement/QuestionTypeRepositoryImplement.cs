using System.Data;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class QuestionTypeRepositoryImplement : IQuestionTypeRepository
{
    private readonly string ConnectionString;
    public QuestionTypeRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }
    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);
    public async Task<QuestionType?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM question_types WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QuestionType>(query, new { Id = id });
    }
    public async Task<QuestionType?> GetByNameAsync(string name)
    {
        const string query = "SELECT * FROM question_types WHERE name = @Name";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<QuestionType>(query, new { Name = name });
    }
    public async Task<IEnumerable<QuestionType>> GetAllAsync()
    {
        const string query = "SELECT * FROM question_types";
        using var conn = CreateConnection();
        return await conn.QueryAsync<QuestionType>(query);
    }
    public async Task<int> AddAsync(QuestionType questionType)
    {
        const string query = "INSERT INTO question_types (name) VALUES (@Name) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, questionType);
    }
    public async Task UpdateAsync(QuestionType questionType)
    {
        const string query = "UPDATE question_types SET name = @Name WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, questionType);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = "DELETE FROM question_types WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }
    public async Task<bool> ExistsByNameAsync(string name)
    {
        const string query = "SELECT COUNT(1) FROM question_types WHERE name = @Name";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(query, new { Name = name });
        return count > 0;
    }
}
