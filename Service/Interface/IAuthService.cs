using ConsoleApp1.Model.DTO.Authentication;
namespace ConsoleApp1.Service.Interface;
public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<bool> LogoutAsync(string accessToken);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> SendForgotPasswordOtpAsync(string email);
    Task<bool> VerifyOtpAsync(string email, string otpCode);
    Task<bool> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword);
}
