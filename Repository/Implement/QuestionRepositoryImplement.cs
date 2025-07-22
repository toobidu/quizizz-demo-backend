using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Questions;
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
    // C?p nh?t phuong th?c AddAsync
    public async Task<int> AddAsync(Question question)
    {
        const string query = @"INSERT INTO questions 
        (question_text, topic_id, question_type_id, created_at, updated_at) 
        VALUES (@QuestionText, @TopicId, @QuestionTypeId, @CreatedAt, @UpdatedAt) 
        RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, question);
    }
    // C?p nh?t phuong th?c UpdateAsync
    public async Task UpdateAsync(Question question)
    {
        const string query = @"UPDATE questions 
        SET question_text = @QuestionText, 
            topic_id = @TopicId, 
            question_type_id = @QuestionTypeId,
            updated_at = @UpdatedAt 
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
    public async Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId)
    {
        const string query = "SELECT * FROM questions WHERE topic_id = @TopicId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Question>(query, new { TopicId = topicId });
    }
    public async Task<IEnumerable<Question>> GetByTypeIdAsync(int typeId)
    {
        const string query = "SELECT * FROM questions WHERE question_type_id = @TypeId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Question>(query, new { TypeId = typeId });
    }
    public async Task<IEnumerable<Question>> GetRandomQuestionsAsync(int count, int? topicId = null)
    {
        string query = topicId.HasValue
            ? "SELECT * FROM questions WHERE topic_id = @TopicId ORDER BY RANDOM() LIMIT @Count"
            : "SELECT * FROM questions ORDER BY RANDOM() LIMIT @Count";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Question>(query, new { TopicId = topicId, Count = count });
    }
    public async Task<IEnumerable<Question>> GetByFiltersAsync(int? topicId, int? typeId)
    {
        string query = @"SELECT * FROM questions WHERE 1=1";
        var parameters = new DynamicParameters();
        if (topicId.HasValue)
        {
            query += " AND topic_id = @TopicId";
            parameters.Add("TopicId", topicId.Value);
        }
        if (typeId.HasValue)
        {
            query += " AND question_type_id = @TypeId";
            parameters.Add("TypeId", typeId.Value);
        }
        using var conn = CreateConnection();
        return await conn.QueryAsync<Question>(query, parameters);
    }
}
