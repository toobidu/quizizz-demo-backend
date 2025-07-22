using ConsoleApp1.Model.Entity.Questions;
namespace ConsoleApp1.Repository.Interface;
public interface IQuestionTypeRepository
{
    Task<QuestionType?> GetByIdAsync(int id);
    Task<QuestionType?> GetByNameAsync(string name);
    Task<IEnumerable<QuestionType>> GetAllAsync();
    Task<int> AddAsync(QuestionType questionType);
    Task UpdateAsync(QuestionType questionType);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
}
