using ConsoleApp1.Model.Entity.Users;
namespace ConsoleApp1.Repository.Interface;
public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(int id);
    Task<Permission?> GetByPermissionNameAsync(string permissionName);
    Task<int> AddAsync(Permission permission);
    Task UpdateAsync(Permission permission);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByPermissionNameAsync(string permissionName);
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<string>> GetPermissionsByUserIdAsync(int userId);
    Task<IEnumerable<Permission>> GetByIdsAsync(IEnumerable<int> ids);
}
