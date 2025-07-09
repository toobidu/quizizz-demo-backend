using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Repository.Interface;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id);
    Task<IEnumerable<Question>> GetAllAsync();
    Task<int> AddAsync(Question question);
    Task UpdateAsync(Question question);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Question>> GetByTopicIdAsync(int topicId);
    Task<IEnumerable<Question>> GetByTypeIdAsync(int typeId);
    Task<IEnumerable<Question>> GetRandomQuestionsAsync(int count, int? topicId = null);

}
