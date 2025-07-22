using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Questions;
namespace ConsoleApp1.Repository.Interface;
public interface ITopicRepository
{
    Task<Topic?> GetByIdAsync(int id);
    Task<Topic?> GetByNameAsync(string name);
    Task<IEnumerable<Topic>> GetAllAsync();
    Task<int> AddAsync(Topic topic);
    Task UpdateAsync(Topic topic);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByNameAsync(string name);
    Task<int> GetQuestionCountAsync(int topicId);
}
