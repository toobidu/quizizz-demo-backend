using ConsoleApp1.Model.Entity.Users;
namespace ConsoleApp1.Repository.Interface;
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(int id);
    Task<Role?> GetByRoleNameAsync(string roleName);
    Task<int> AddAsync(Role role);
    Task UpdateAsync(Role role);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByRoleNameAsync(string roleName);
    Task<IEnumerable<Role>> GetAllAsync();
}
