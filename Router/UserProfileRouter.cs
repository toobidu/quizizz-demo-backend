using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Config;
using ConsoleApp1.Router;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Router;

public class UserProfileRouter : IBaseRouter
{
    private readonly UserProfileController _controller;

    public UserProfileRouter(UserProfileController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/profile")) return false;

        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
            return true;
        }

        try
        {
            switch (method.ToUpper())
            {
                case "GET" when path == "/api/profile/me":
                    await GetMyProfile(response, path);
                    return true;
                case "GET" when path.StartsWith("/api/profile/search/"):
                    await SearchUser(response, path);
                    return true;
                case "PUT" when path == "/api/profile/password":
                    await ChangePassword(request, response, path);
                    return true;
                case "PUT" when path == "/api/profile/update":
                    await UpdateProfile(request, response, path);
                    return true;
                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task GetMyProfile(HttpListenerResponse response, string path)
    {
        var result = await _controller.GetMyProfileAsync(1); // TODO: Get userId from token
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task SearchUser(HttpListenerResponse response, string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 5)
        {
            HttpResponseHelper.WriteBadRequest(response, "Tên người dùng không hợp lệ", path);
            return;
        }

        var username = parts[4];
        var result = await _controller.SearchUserAsync(username, 1); // TODO: Get userId from token
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task ChangePassword(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var requestData = await ParseJson<ChangePasswordRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Yêu cầu đổi mật khẩu không hợp lệ", path);
            return;
        }

        var result = await _controller.ChangePasswordAsync(1, requestData); // TODO: Get userId from token
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task UpdateProfile(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var requestData = await ParseJson<UpdateProfileRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Thông tin cập nhật không hợp lệ", path);
            return;
        }

        var result = await _controller.UpdateProfileAsync(1, requestData); // TODO: Get userId from token
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private static string? GetAccessToken(HttpListenerRequest request)
    {
        string? authHeader = request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;
        return authHeader["Bearer ".Length..].Trim();
    }

    private static async Task<T?> ParseJson<T>(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body);
    }
}