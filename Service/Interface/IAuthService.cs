using ConsoleApp1.Model.DTO;

namespace ConsoleApp1.Service.Interface;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<bool> LogoutAsync(string accessToken);
}
