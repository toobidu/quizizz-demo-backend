using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Authentication;

namespace ConsoleApp1.Router;

public class AuthRouter : IBaseRouter
{
    private readonly AuthController _authController;

    public AuthRouter(AuthController authController)
    {
        _authController = authController;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/auth")) return false;

        try
        {
            switch ((method, path))
            {
                case ("POST", "/api/auth/register"):
                {
                    var registerDto = await Deserialize<RegisterRequest>(request);
                    if (registerDto == null)
                    {
                        HttpResponseHelper.WriteBadRequest(response, "Dữ liệu đăng ký không hợp lệ", path);
                        return true;
                    }

                    var apiResponse = await _authController.RegisterApi(registerDto);
                    HttpResponseHelper.WriteJsonResponse(response, apiResponse);
                    return true;
                }

                case ("POST", "/api/auth/login"):
                {
                    var loginDto = await Deserialize<LoginRequest>(request);
                    if (loginDto == null)
                    {
                        HttpResponseHelper.WriteBadRequest(response, "Dữ liệu đăng nhập không hợp lệ", path);
                        return true;
                    }

                    var apiResponse = await _authController.LoginApi(loginDto);
                    HttpResponseHelper.WriteJsonResponse(response, apiResponse);
                    return true;
                }

                case ("POST", "/api/auth/logout"):
                {
                    var jsonDoc = await ParseJson(request);
                    if (!jsonDoc.RootElement.TryGetProperty("token", out var tokenElement))
                    {
                        HttpResponseHelper.WriteBadRequest(response, "Thiếu token đăng xuất", path);
                        return true;
                    }

                    string token = tokenElement.GetString()!;
                    var apiResponse = await _authController.LogoutApi(token);
                    HttpResponseHelper.WriteJsonResponse(response, apiResponse);
                    return true;
                }

                default:
                    HttpResponseHelper.WriteNotFound(response, "Không tìm thấy API yêu cầu", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private static async Task<T?> Deserialize<T>(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body);
    }

    private static async Task<JsonDocument> ParseJson(HttpListenerRequest request)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        return JsonDocument.Parse(body);
    }
}