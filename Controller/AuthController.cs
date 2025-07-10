using ConsoleApp1.Model.DTO.Authentication;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Config;

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
    public async Task<ApiResponse<string>> RegisterApi(RegisterRequest request)
    {
        try
        {
            if (!request.ValidField())
                return ApiResponse<string>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA", "/api/auth/register");

            bool success = await _authService.RegisterAsync(request);
            if (success)
                return ApiResponse<string>.Success("Đăng ký thành công", "Đăng ký thành công", 200, "/api/auth/register");
            else
                return ApiResponse<string>.Fail("Đăng ký thất bại", 500, "REGISTER_FAILED", "/api/auth/register");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/auth/register");
        }
    }

    /*
    POST /api/auth/login
    */
    public async Task<ApiResponse<LoginResponse>> LoginApi(LoginRequest request)
    {
        if (!request.ValidField())
            return ApiResponse<LoginResponse>.Fail("Dữ liệu đăng nhập không hợp lệ", 400, "INVALID_CREDENTIALS", "/api/auth/login");

        var result = await _authService.LoginAsync(request);
        if (result == null)
            return ApiResponse<LoginResponse>.Fail("Tên đăng nhập hoặc mật khẩu không đúng", 401, "AUTH_FAILED", "/api/auth/login");

        return ApiResponse<LoginResponse>.Success(result, "Đăng nhập thành công", 200, "/api/auth/login");
    }

    /*
    POST /api/auth/logout
    */
    public async Task<ApiResponse<string>> LogoutApi(string accessToken)
    {
        bool success = await _authService.LogoutAsync(accessToken);
        if (success)
            return ApiResponse<string>.Success("Đăng xuất thành công", "Đăng xuất thành công", 200, "/api/auth/logout");
        else
            return ApiResponse<string>.Fail("Token không hợp lệ hoặc đã hết hạn", 400, "INVALID_TOKEN", "/api/auth/logout");
    }


}