using System.Data;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class TopicRepositoryImplement : ITopicRepository
{
    private readonly string ConnectionString;

    public TopicRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<Topic?> GetByIdAsync(int id)
    {
        const string query = "SELECT * FROM topics WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Topic>(query, new { Id = id });
    }

    public async Task<Topic?> GetByNameAsync(string name)
    {
        const string query = "SELECT * FROM topics WHERE name = @Name";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Topic>(query, new { Name = name });
    }

    public async Task<IEnumerable<Topic>> GetAllAsync()
    {
        const string query = "SELECT * FROM topics";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Topic>(query);
    }

    public async Task<int> AddAsync(Topic topic)
    {
        const string query = "INSERT INTO topics (name) VALUES (@Name) RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, topic);
    }

    public async Task UpdateAsync(Topic topic)
    {
        const string query = "UPDATE topics SET name = @Name WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, topic);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string query = "DELETE FROM topics WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        const string query = "SELECT COUNT(1) FROM topics WHERE name = @Name";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(query, new { Name = name });
        return count > 0;
    }

    public async Task<int> GetQuestionCountAsync(int topicId)
    {
        const string query = "SELECT COUNT(*) FROM questions WHERE topic_id = @TopicId";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, new { TopicId = topicId });
    }
}