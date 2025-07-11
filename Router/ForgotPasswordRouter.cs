using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Authentication;

namespace ConsoleApp1.Router;

public class ForgotPasswordRouter : IBaseRouter
{
    private readonly ForgotPasswordController _controller;

    public ForgotPasswordRouter(ForgotPasswordController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/forgot-password")) return false;
        
        Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Handling request: {method} {path}");

        try
        {
            switch ((method.ToUpper(), path))
            {
                case ("POST", "/api/forgot-password/send-otp"):
                    await HandleSendOtp(request, response);
                    return true;

                case ("POST", "/api/forgot-password/verify-otp"):
                    await HandleVerifyOtp(request, response);
                    return true;

                case ("POST", "/api/forgot-password/reset"):
                    await HandleResetPassword(request, response);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "API không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Exception: {ex.Message}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task HandleSendOtp(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Processing send OTP request");
            
            // Log raw request body
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Raw request body: '{body}'");
            
            if (string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Request body is empty");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu yêu cầu trống", "/api/forgot-password/send-otp");
                return;
            }

            ForgotPasswordRequest? forgotPasswordDto;
            try
            {
                forgotPasswordDto = JsonSerializer.Deserialize<ForgotPasswordRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Deserialized email: '{forgotPasswordDto?.Email ?? "null"}'");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] JSON deserialization error: {ex.Message}");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu JSON không hợp lệ", "/api/forgot-password/send-otp");
                return;
            }

            if (forgotPasswordDto == null)
            {
                Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Deserialized object is null");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu không hợp lệ", "/api/forgot-password/send-otp");
                return;
            }

            var apiResponse = await _controller.SendOtpAsync(forgotPasswordDto);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] HandleSendOtp error: {ex.Message}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, "/api/forgot-password/send-otp");
        }
    }

    private async Task HandleVerifyOtp(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] VerifyOtp request body: '{body}'");
        
        var verifyOtpDto = JsonSerializer.Deserialize<VerifyOtpRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Parsed - Email: '{verifyOtpDto?.Email}', OTP: '{verifyOtpDto?.OtpCode}'");
        
        if (verifyOtpDto == null)
        {
            Console.WriteLine("[FORGOT_PASSWORD_ROUTER] VerifyOtpDto is null");
            HttpResponseHelper.WriteBadRequest(response, "Dữ liệu không hợp lệ", "/api/forgot-password/verify-otp");
            return;
        }

        var apiResponse = await _controller.VerifyOtpAsync(verifyOtpDto);
        Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Response: Success={apiResponse.Message}, Status={apiResponse.Status}");
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleResetPassword(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Processing reset password request");
            
            // Log raw request body
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Reset password raw body: '{body}'");
            
            if (string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Reset password body is empty");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu yêu cầu trống", "/api/forgot-password/reset");
                return;
            }

            ResetPasswordRequest? resetPasswordDto;
            try
            {
                resetPasswordDto = JsonSerializer.Deserialize<ResetPasswordRequest>(body, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Deserialized reset data - Email: '{resetPasswordDto?.Email}', NewPassword length: {resetPasswordDto?.NewPassword?.Length ?? 0}, OtpCode: '{resetPasswordDto?.OtpCode}' (length: {resetPasswordDto?.OtpCode?.Length ?? 0}), ConfirmPassword length: {resetPasswordDto?.ConfirmPassword?.Length ?? 0}");
                Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Validation result: {resetPasswordDto?.ValidField()}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Reset password JSON error: {ex.Message}");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu JSON không hợp lệ", "/api/forgot-password/reset");
                return;
            }
            
            if (resetPasswordDto == null)
            {
                Console.WriteLine("[FORGOT_PASSWORD_ROUTER] Reset password DTO is null");
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu không hợp lệ", "/api/forgot-password/reset");
                return;
            }

            var apiResponse = await _controller.ResetPasswordAsync(resetPasswordDto);
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] Reset password response: Success={apiResponse.Message}, Status={apiResponse.Status}, Message={apiResponse.Message}");
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FORGOT_PASSWORD_ROUTER] HandleResetPassword error: {ex.Message}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, "/api/forgot-password/reset");
        }
    }

    private static async Task<T?> Deserialize<T>(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        if (string.IsNullOrWhiteSpace(body))
            return default(T);

        try
        {
            return JsonSerializer.Deserialize<T>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return default(T);
        }
    }
}