using ConsoleApp1.Model.DTO.Users;

namespace ConsoleApp1.Service.Interface;

public interface IUserService
{
    
    Task<bool> CreateUserAsync(UserDTO user);
    Task<List<UserDTO>> GetAllUsersAsync();
    Task<UserDTO?> GetUserByIdAsync(int userId);
    Task<bool> UpdateUserAsync(int userId, UserDTO updatedUser);
    Task<bool> DeleteUserAsync(int userId);
    Task<bool> UpdateUserTypeAccountAsync(int userId, string newTypeAccount);
    Task<string?> GetTypeAccountAsync(int userId);
    Task<int> MapTypeAccountToRoleIdAsync(string typeAccount);
}
