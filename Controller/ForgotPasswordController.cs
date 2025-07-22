using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Authentication;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class ForgotPasswordController
{
    private readonly IAuthService _authService;
    public ForgotPasswordController(IAuthService authService)
    {
        _authService = authService;
    }
    public async Task<ApiResponse<string>> SendOtpAsync(ForgotPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<string>.Fail("Yêu cầu không hợp lệ", 400, "INVALID_REQUEST", "/api/forgot-password/send-otp");
            }
            if (!request.ValidField())
            {
                return ApiResponse<string>.Fail("Email không hợp lệ", 400, "INVALID_EMAIL", "/api/forgot-password/send-otp");
            }
            bool success = await _authService.SendForgotPasswordOtpAsync(request.Email);
            if (success)
                return ApiResponse<string>.Success("Mã OTP đã được gửi đến email của bạn", "OTP sent successfully", 200, "/api/forgot-password/send-otp");
            else
                return ApiResponse<string>.Fail("Email không tồn tại trong hệ thống", 404, "EMAIL_NOT_FOUND", "/api/forgot-password/send-otp");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/forgot-password/send-otp");
        }
    }
    public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpRequest request)
    {
        try
        {
            if (request == null || !request.ValidField())
                return ApiResponse<string>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA", "/api/forgot-password/verify-otp");
            bool success = await _authService.VerifyOtpAsync(request.Email, request.OtpCode);
            if (success)
                return ApiResponse<string>.Success("Mã OTP hợp lệ", "OTP verified successfully", 200, "/api/forgot-password/verify-otp");
            else
                return ApiResponse<string>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn", 400, "INVALID_OTP", "/api/forgot-password/verify-otp");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/forgot-password/verify-otp");
        }
    }
    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            if (request == null || !request.ValidField())
                return ApiResponse<string>.Fail("Dữ liệu không hợp lệ", 400, "INVALID_DATA", "/api/forgot-password/reset");
            bool success = await _authService.ResetPasswordWithOtpAsync(request.Email, request.OtpCode, request.NewPassword);
            if (success)
                return ApiResponse<string>.Success("Đặt lại mật khẩu thành công", "Password reset successfully", 200, "/api/forgot-password/reset");
            else
                return ApiResponse<string>.Fail("Đặt lại mật khẩu thất bại", 400, "RESET_FAILED", "/api/forgot-password/reset");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/forgot-password/reset");
        }
    }
}
