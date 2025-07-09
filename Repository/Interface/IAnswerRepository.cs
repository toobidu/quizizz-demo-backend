using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Repository.Interface;

public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(int id);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
    Task<int> AddAsync(Answer answer);
    Task UpdateAsync(Answer answer);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByQuestionIdAsync(int questionId);
}