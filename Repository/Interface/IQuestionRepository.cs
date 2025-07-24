using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Model.DTO.Questions;

namespace ConsoleApp1.Repository.Interface;

public interface IQuestionRepository
{
    Task<Question?> GetQuestionByIdAsync(int questionId);
    Task<List<Question>> GetQuestionsByTopicIdAsync(int topicId);
    Task<List<Question>> GetQuestionsByTopicNameAsync(string topicName);
    Task<bool> CheckAnswerCorrectAsync(int questionId, int answerId);
    Task<int> CreateQuestionAsync(Question question);
    Task<bool> UpdateQuestionAsync(Question question);
    Task<bool> DeleteQuestionAsync(int questionId);
    
    // Các phương thức tương thích cũ
    Task<Question?> GetByIdAsync(int id);
    Task<IEnumerable<Question>> GetAllAsync();
    Task<int> AddAsync(Question question);
    Task UpdateAsync(Question question);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId);
    Task<IEnumerable<Question>> GetByTypeIdAsync(int typeId);
    Task<IEnumerable<Question>> GetRandomQuestionsAsync(int count, int? topicId = null);
    Task<IEnumerable<Question>> GetByFiltersAsync(int? topicId, int? typeId);
    Task<IEnumerable<QuestionAnswerRawDTO>> GetQuestionsWithAnswersByTopicNameAsync(string topicName);
}