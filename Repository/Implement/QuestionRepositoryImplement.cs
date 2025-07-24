using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class QuestionRepositoryImplement : IQuestionRepository
{
    private readonly DatabaseHelper _dbHelper;
    public QuestionRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    
    public async Task<Question?> GetQuestionByIdAsync(int questionId)
    {
        const string query = @"SELECT * FROM questions WHERE id = @QuestionId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Question>(query, new { QuestionId = questionId });
    }
    
    public async Task<List<Question>> GetQuestionsByTopicIdAsync(int topicId)
    {
        const string query = "SELECT * FROM questions WHERE topic_id = @TopicId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Question>(query, new { TopicId = topicId });
        return result.ToList();
    }
    
    public async Task<List<Question>> GetQuestionsByTopicNameAsync(string topicName)
    {
        const string query = @"
            SELECT q.* 
            FROM questions q
            JOIN topics t ON q.topic_id = t.id
            WHERE t.name = @TopicName";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Question>(query, new { TopicName = topicName });
        return result.ToList();
    }
    
    public async Task<bool> CheckAnswerCorrectAsync(int questionId, int answerId)
    {
        const string query = @"
            SELECT is_correct 
            FROM answers 
            WHERE question_id = @QuestionId AND id = @AnswerId";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(query, new { QuestionId = questionId, AnswerId = answerId });
    }
    
    public async Task<int> CreateQuestionAsync(Question question)
    {
        const string query = @"INSERT INTO questions 
        (question_text, topic_id, question_type_id) 
        VALUES (@QuestionText, @TopicId, @QuestionTypeId) 
        RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, question);
    }
    
    public async Task<bool> UpdateQuestionAsync(Question question)
    {
        const string query = @"UPDATE questions 
        SET question_text = @QuestionText, 
            topic_id = @TopicId, 
            question_type_id = @QuestionTypeId
        WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, question);
        return affected > 0;
    }
    
    public async Task<bool> DeleteQuestionAsync(int questionId)
    {
        const string query = @"DELETE FROM questions WHERE id = @QuestionId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { QuestionId = questionId });
        return affected > 0;
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<Question?> GetByIdAsync(int id)
    {
        return await GetQuestionByIdAsync(id);
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
        return await CreateQuestionAsync(question);
    }
    
    public async Task UpdateAsync(Question question)
    {
        await UpdateQuestionAsync(question);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        return await DeleteQuestionAsync(id);
    }
    
    public async Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId)
    {
        return await GetQuestionsByTopicIdAsync(topicId);
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

    /// <summary>
    /// Lấy danh sách câu hỏi kèm theo câu trả lời theo tên chủ đề
    /// Sử dụng JOIN query để kết hợp dữ liệu từ 3 bảng: topics, questions, answers
    /// </summary>
    /// <param name="topicName">Tên chủ đề cần lấy câu hỏi (ví dụ: "Toán học", "Lịch sử")</param>
    /// <returns>Danh sách raw data từ JOIN query, cần được group lại ở tầng service</returns>
    public async Task<IEnumerable<QuestionAnswerRawDTO>> GetQuestionsWithAnswersByTopicNameAsync(string topicName)
    {
        const string query = @"
            SELECT 
                t.name AS TopicName,
                q.id AS QuestionId,
                q.question_text AS QuestionText,
                a.id AS AnswerId,
                a.answer_text AS AnswerText,
                a.is_correct AS IsCorrect
            FROM questions q
            JOIN topics t ON q.topic_id = t.id
            JOIN answers a ON q.id = a.question_id
            WHERE t.name = @TopicName
            ORDER BY q.id, a.id";
        
        using var conn = CreateConnection();
        return await conn.QueryAsync<QuestionAnswerRawDTO>(query, new { TopicName = topicName });
    }
}
