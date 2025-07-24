using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Repository.Interface;

public interface IUserRepository
{
    Task<User?> GetUserByIdAsync(int userId);
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByEmailAsync(string email);
    Task<List<User>> GetAllUsersAsync(int page, int limit);
    Task<int> CreateUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(int userId);
    Task<User?> GetByPhoneNumberAsync(string phoneNumber);
    
    // Các phương thức tương thích cũ
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<int> AddAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
}