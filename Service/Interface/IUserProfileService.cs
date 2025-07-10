using ConsoleApp1.Model.DTO.Users;

namespace ConsoleApp1.Service.Interface;

public interface IUserProfileService
{
    Task<UserProfileDTO?> GetUserProfileAsync(int userId);
    Task<UserProfileDTO?> SearchUserByUsernameAsync(string username);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request);
}