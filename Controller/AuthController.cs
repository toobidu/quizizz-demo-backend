using ConsoleApp1.Model.DTO;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class AuthController
{
    private readonly IAuthService _authService;
    private readonly JwtHelper _jwtHelper;

    public AuthController(IAuthService authService, JwtHelper jwtHelper)
    {
        _authService = authService;
        _jwtHelper = jwtHelper;
    }

    /*
    POST /api/auth/register
    */
    public async Task<string> RegisterApi(RegisterRequest request)
    {
        if (!request.ValidField())
            return "Dữ liệu không hợp lệ";

        bool success = await _authService.RegisterAsync(request);
        return success ? "Đăng ký thành công" : "Tên người dùng đã tồn tại";
    }

    /*
    POST /api/auth/login
    */
    public async Task<LoginResponse?> LoginApi(LoginRequest request)
    {
        if (!request.ValidField())
            return null;

        return await _authService.LoginAsync(request);
    }

    /*
    POST /api/auth/logout
    */
    public async Task<string> LogoutApi(string accessToken)
    {
        bool success = await _authService.LogoutAsync(accessToken);
        return success ? "Đăng xuất thành công" : "Token không hợp lệ hoặc đã hết hạn";
    }
}