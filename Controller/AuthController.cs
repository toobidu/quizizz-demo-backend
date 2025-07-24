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

    /// <summary>
    /// API đăng ký tài khoản mới
    /// </summary>
    /// <param name="request">Thông tin đăng ký</param>
    /// <returns>Kết quả đăng ký</returns>
    public async Task<ApiResponse<string>> RegisterApi(RegisterRequest request)
    {
        try
        {
            if (!request.ValidField())
                return ApiResponse<string>.Fail("D? li?u kh�ng h?p l?", 400, "INVALID_DATA", "/api/auth/register");
            bool success = await _authService.RegisterAsync(request);
            if (success)
                return ApiResponse<string>.Success("�ang k� th�nh c�ng", "�ang k� th�nh c�ng", 200, "/api/auth/register");
            else
                return ApiResponse<string>.Fail("�ang k� th?t b?i", 500, "REGISTER_FAILED", "/api/auth/register");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("L?i m�y ch?: " + ex.Message, 500, "SERVER_ERROR", "/api/auth/register");
        }
    }
    /// <summary>
    /// API đăng nhập vào hệ thống
    /// </summary>
    /// <param name="request">Thông tin đăng nhập</param>
    /// <returns>JWT token và thông tin user</returns>
    public async Task<ApiResponse<LoginResponse>> LoginApi(LoginRequest request)
    {
        if (!request.ValidField())
            return ApiResponse<LoginResponse>.Fail("D? li?u dang nh?p kh�ng h?p l?", 400, "INVALID_CREDENTIALS", "/api/auth/login");
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return ApiResponse<LoginResponse>.Fail("T�n dang nh?p ho?c m?t kh?u kh�ng d�ng", 401, "AUTH_FAILED", "/api/auth/login");
        return ApiResponse<LoginResponse>.Success(result, "�ang nh?p th�nh c�ng", 200, "/api/auth/login");
    }
    /// <summary>
    /// API đăng xuất khỏi hệ thống
    /// </summary>
    /// <param name="accessToken">JWT access token</param>
    /// <returns>Kết quả đăng xuất</returns>
    public async Task<ApiResponse<string>> LogoutApi(string accessToken)
    {
        bool success = await _authService.LogoutAsync(accessToken);
        if (success)
            return ApiResponse<string>.Success("�ang xu?t th�nh c�ng", "�ang xu?t th�nh c�ng", 200, "/api/auth/logout");
        else
            return ApiResponse<string>.Fail("Token kh�ng h?p l? ho?c d� h?t h?n", 400, "INVALID_TOKEN", "/api/auth/logout");
    }
}
