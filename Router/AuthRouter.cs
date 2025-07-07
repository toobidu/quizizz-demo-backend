using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO;

namespace ConsoleApp1.Router;

public class AuthRouter : IBaseRouter
{
    private readonly AuthController _authController;

    public AuthRouter(AuthController authController) => _authController = authController;

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
                    var registerDto = await Deserialize<RegisterRequest>(request);
                    var regResult = await _authController.RegisterApi(registerDto!);
                    await WriteJson(response, new { message = regResult });
                    return true;

                case ("POST", "/api/auth/login"):
                    var loginDto = await Deserialize<LoginRequest>(request);
                    var loginResult = await _authController.LoginApi(loginDto!);
                    if (loginResult == null)
                    {
                        response.StatusCode = 401;
                        await WriteJson(response, new { error = "Đăng nhập thất bại" });
                    }
                    else
                    {
                        await WriteJson(response, loginResult);
                    }

                    return true;

                case ("POST", "/api/auth/logout"):
                    var jsonDoc = await ParseJson(request);
                    var token = jsonDoc.RootElement.GetProperty("token").GetString()!;
                    var logoutResult = await _authController.LogoutApi(token);
                    await WriteJson(response, new { message = logoutResult });
                    return true;

                default:
                    response.StatusCode = 404;
                    await WriteJson(response, new { error = "404 Not Found" });
                    return true;
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteJson(response, new { error = "Internal Server Error", detail = ex.Message });
            return true;
        }
    }

    private static async Task<T?> Deserialize<T>(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        return JsonSerializer.Deserialize<T>(await reader.ReadToEndAsync());
    }

    private static async Task<JsonDocument> ParseJson(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        return JsonDocument.Parse(await reader.ReadToEndAsync());
    }

    private static async Task WriteJson(HttpListenerResponse res, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var buffer = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json";
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = buffer.Length;
        await res.OutputStream.WriteAsync(buffer);
    }
}