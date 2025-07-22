using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Config;
using ConsoleApp1.Router;
using ConsoleApp1.Security;
using System.Net;
using System.Text;
using System.Text.Json;
namespace ConsoleApp1.Router;
public class UserProfileRouter : IBaseRouter
{
    private readonly UserProfileController _controller;
    private readonly JwtHelper _jwtHelper;
    public UserProfileRouter(UserProfileController controller, JwtHelper jwtHelper)
    {
        _controller = controller;
        _jwtHelper = jwtHelper;
    }
    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        if (!path.StartsWith("/api/profile")) return false;
        // Handle CORS preflight request
        if (method.ToUpper() == "OPTIONS")
        {
            HttpResponseHelper.WriteOptionsResponse(response);
            return true;
        }
        string? token = GetAccessToken(request);
        if (token == null || token == "null")
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
            return true;
        }
        int? userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return true;
        }
        try
        {
            switch (method.ToUpper())
            {
                case "GET" when path == "/api/profile/me":
                    await GetMyProfile(response, path, userId.Value);
                    return true;
                case "GET" when path.StartsWith("/api/profile/search/"):
                    await SearchUser(response, path, userId.Value);
                    return true;
                case "PUT" when path == "/api/profile/password":
                    await ChangePassword(request, response, path, userId.Value);
                    return true;
                case "PUT" when path == "/api/profile/update":
                    await UpdateProfile(request, response, path, userId.Value);
                    return true;
                case "PUT" when path.StartsWith("/api/profile/") && path.EndsWith("/update"):
                    await UpdateProfileById(request, response, path, userId.Value);
                    return true;
                case "PUT" when path.StartsWith("/api/profile/") && path.EndsWith("/password"):
                    await ChangePasswordById(request, response, path, userId.Value);
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
    private async Task GetMyProfile(HttpListenerResponse response, string path, int userId)
    {
        try
        {
            var result = await _controller.GetMyProfileAsync(userId);
            HttpResponseHelper.WriteJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    private async Task SearchUser(HttpListenerResponse response, string path, int currentUserId)
    {
        var parts = path.Split('/');
        if (parts.Length < 5)
        {
            HttpResponseHelper.WriteBadRequest(response, "Tên người dùng không hợp lệ", path);
            return;
        }
        var username = parts[4];
        var result = await _controller.SearchUserAsync(username, currentUserId);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task ChangePassword(HttpListenerRequest request, HttpListenerResponse response, string path, int userId)
    {
        var requestData = await ParseJson<ChangePasswordRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Yêu cầu đổi mật khẩu không hợp lệ", path);
            return;
        }
        var result = await _controller.ChangePasswordAsync(userId, requestData);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task UpdateProfile(HttpListenerRequest request, HttpListenerResponse response, string path, int userId)
    {
        var requestData = await ParseJson<UpdateProfileRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Thông tin cập nhật không hợp lệ", path);
            return;
        }
        var result = await _controller.UpdateProfileAsync(userId, requestData);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private static string? GetAccessToken(HttpListenerRequest request)
    {
        string? authHeader = request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;
        return authHeader["Bearer ".Length..].Trim();
    }
    private async Task UpdateProfileById(HttpListenerRequest request, HttpListenerResponse response, string path, int currentUserId)
    {
        // Lấy profileId từ URL: /api/profile/{profileId}/update
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int profileId))
        {
            HttpResponseHelper.WriteBadRequest(response, "ID hồ sơ không hợp lệ", path);
            return;
        }
        var requestData = await ParseJson<UpdateProfileRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Thông tin cập nhật không hợp lệ", path);
            return;
        }
        var result = await _controller.UpdateProfileByIdAsync(profileId, currentUserId, requestData);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task ChangePasswordById(HttpListenerRequest request, HttpListenerResponse response, string path, int currentUserId)
    {
        // Lấy profileId từ URL: /api/profile/{profileId}/password
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int profileId))
        {
            HttpResponseHelper.WriteBadRequest(response, "ID hồ sơ không hợp lệ", path);
            return;
        }
        var requestData = await ParseJson<ChangePasswordRequest>(request);
        if (requestData == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Yêu cầu đổi mật khẩu không hợp lệ", path);
            return;
        }
        var result = await _controller.ChangePasswordByIdAsync(profileId, currentUserId, requestData);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private static async Task<T?> ParseJson<T>(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<T>(body, options);
        return result;
    }
}
