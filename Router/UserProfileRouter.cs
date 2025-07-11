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
        
        Console.WriteLine($"[UserProfile] Request: {method} {path} from {request.RemoteEndPoint}");

        if (!path.StartsWith("/api/profile")) return false;

        // Handle CORS preflight request
        if (method.ToUpper() == "OPTIONS")
        {
            Console.WriteLine($"[UserProfile] Handling CORS preflight for {path}");
            HttpResponseHelper.WriteOptionsResponse(response);
            return true;
        }

        string? token = GetAccessToken(request);
        if (token == null || token == "null")
        {
            Console.WriteLine($"[UserProfile] Unauthorized access to {path} - Missing or invalid token");
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
            return true;
        }

        int? userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            Console.WriteLine($"[UserProfile] Invalid token for {path}");
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return true;
        }
        
        Console.WriteLine($"[UserProfile] Token found for {path}, userId: {userId}");

        try
        {
            switch (method.ToUpper())
            {
                case "GET" when path == "/api/profile/me":
                    Console.WriteLine($"[UserProfile] Processing GetMyProfile request");
                    await GetMyProfile(response, path, userId.Value);
                    return true;
                case "GET" when path.StartsWith("/api/profile/search/"):
                    Console.WriteLine($"[UserProfile] Processing SearchUser request");
                    await SearchUser(response, path, userId.Value);
                    return true;
                case "PUT" when path == "/api/profile/password":
                    Console.WriteLine($"[UserProfile] Processing ChangePassword request");
                    await ChangePassword(request, response, path, userId.Value);
                    return true;
                case "PUT" when path == "/api/profile/update":
                    Console.WriteLine($"[UserProfile] Processing UpdateProfile request");
                    await UpdateProfile(request, response, path, userId.Value);
                    return true;
                case "PUT" when path.StartsWith("/api/profile/") && path.EndsWith("/update"):
                    Console.WriteLine($"[UserProfile] Processing UpdateProfileById request");
                    await UpdateProfileById(request, response, path, userId.Value);
                    return true;
                case "PUT" when path.StartsWith("/api/profile/") && path.EndsWith("/password"):
                    Console.WriteLine($"[UserProfile] Processing ChangePasswordById request");
                    await ChangePasswordById(request, response, path, userId.Value);
                    return true;
                default:
                    Console.WriteLine($"[UserProfile] Unhandled route: {method} {path}");
                    return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error processing {method} {path}: {ex.Message}");
            Console.WriteLine($"[UserProfile] Stack trace: {ex.StackTrace}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task GetMyProfile(HttpListenerResponse response, string path, int userId)
    {
        try
        {
            Console.WriteLine($"[UserProfile] Calling GetMyProfileAsync for userId={userId}");
            var result = await _controller.GetMyProfileAsync(userId);
            Console.WriteLine($"[UserProfile] GetMyProfile result: Success={result.Message}, Message={result.Message}");
            HttpResponseHelper.WriteJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UserProfile] Error in GetMyProfile: {ex.Message}");
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

        Console.WriteLine($"[UserProfile] Update request data - FullName: '{requestData.FullName}', PhoneNumber: '{requestData.PhoneNumber}', Address: '{requestData.Address}'");
        var result = await _controller.UpdateProfileAsync(userId, requestData);
        Console.WriteLine($"[UserProfile] Update result - Success: {result.Message}, Message: {result.Message}");
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
        Console.WriteLine($"[UserProfile] Request body: {body}");
        
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        
        var result = JsonSerializer.Deserialize<T>(body, options);
        Console.WriteLine($"[UserProfile] Deserialized: {JsonSerializer.Serialize(result, options)}");
        return result;
    }
}