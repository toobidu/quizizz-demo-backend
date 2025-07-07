using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRedisService _redisService;
    private readonly JwtHelper _jwt;
    private readonly SecurityConfig _security;

    public AuthService(IUserRepository userRepo, IRedisService redisService, JwtHelper jwt, SecurityConfig security)
    {
        _userRepo = userRepo;
        _redisService = redisService;
        _jwt = jwt;
        _security = security;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        if (!request.ValidField())
            return false;

        if (await _userRepo.ExistsByUsernameAsync(request.Username))
            return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = new User(request.Username, hash, request.TypeAccount);

        await _userRepo.AddAsync(user);

        return true;
    }
    
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        if (!request.ValidField())
        {
            Console.WriteLine("Invalid login fields");
            return null;
        }

        var user = await _userRepo.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            Console.WriteLine($"User not found: {request.Username}");
            return null;
        }

        bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
        Console.WriteLine($"Password verification result: {passwordValid}");

        if (!passwordValid)
            return null;

        try
        {
            string accessToken = _jwt.GenerateAccessToken(user.Id, user.Username, user.TypeAccount);
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("Failed to generate access token");
                return null;
            }

            string refreshToken = _jwt.GenerateRefreshToken();
            if (string.IsNullOrEmpty(refreshToken))
            {
                Console.WriteLine("Failed to generate refresh token");
                return null;
            }

            await _redisService.SetRefreshTokenAsync(user.Id, refreshToken, 
                TimeSpan.FromDays(_security.RefreshTokenExpirationDays));

            return new LoginResponse(accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during token generation: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> LogoutAsync(string accessToken)
    {
        int? userId = _jwt.GetUserIdFromToken(accessToken);
        if (userId == null)
            return false;

        await _redisService.DeleteRefreshTokenAsync(userId.Value);
        return true;
    }
}
