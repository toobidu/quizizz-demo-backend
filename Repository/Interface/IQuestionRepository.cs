using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IQuestionRepository
{
    Task<Question?> GetByIdAsync(int id);
    Task<IEnumerable<Question>> GetAllAsync();
    Task<int> AddAsync(Question question);
    Task UpdateAsync(Question question);
    Task<bool> DeleteAsync(int id);
}
