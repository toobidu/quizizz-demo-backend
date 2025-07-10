using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Repository.Interface;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<int> AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
}
