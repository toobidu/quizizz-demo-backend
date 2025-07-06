using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Repository.Interface;

public interface IUserRoleRepository
{
    Task<UserRole?> GetByUserIdAndRoleIdAsync(int userId, int roleId);
    Task<IEnumerable<UserRole>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId);
    Task<int> AddAsync(UserRole userRole);
    Task<bool> DeleteByUserIdAsync(int userId);
    Task<bool> DeleteByUserIdAndRoleIdAsync(int userId, int roleId);
}