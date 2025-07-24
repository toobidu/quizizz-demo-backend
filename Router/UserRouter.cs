using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Users;
namespace ConsoleApp1.Router;
public class UserRouter : IBaseRouter
{
    private readonly UserController _userController;
    public UserRouter(UserController userController)
    {
        _userController = userController;
    }
    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        if (!path.StartsWith("/api/users")) return false;
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thi?u ho?c sai token x�c th?c", path);
            return true;
        }
        try
        {
            // POST /api/users
            if (method == "POST" && path == "/api/users")
            {
                var dto = await ParseJson<UserDTO>(request);
                var message = await _userController.CreateUserAsync(dto, token);
                HttpResponseHelper.WriteSuccess(response, new { message }, message, path);
                return true;
            }
            // GET /api/users
            if (method == "GET" && path == "/api/users")
            {
                var result = await _userController.GetAllUsersAsync(token);
                HttpResponseHelper.WriteSuccess(response, result, "Lấy danh sách người dùng thành công", path);
                return true;
            }
            // GET /api/users/{userId}
            if (method == "GET" && path.StartsWith("/api/users/") &&
                int.TryParse(path.Split("/")[3], out int getId) &&
                !path.Contains("type-account"))
            {
                var result = await _userController.GetUserByIdAsync(getId, token);
                if (result == null)
                    HttpResponseHelper.WriteNotFound(response, "Không tìm thấy người dùng", path);
                else
                    HttpResponseHelper.WriteSuccess(response, result, "Lấy thông tin người dùng thành công", path);
                return true;
            }
            // PUT /api/users/{userId}
            if (method == "PUT" && path.StartsWith("/api/users/") &&
                int.TryParse(path.Split("/")[3], out int updateId) &&
                !path.Contains("type-account"))
            {
                var dto = await ParseJson<UserDTO>(request);
                var message = await _userController.UpdateUserAsync(updateId, dto, token);
                HttpResponseHelper.WriteSuccess(response, new { message }, message, path);
                return true;
            }
            // DELETE /api/users/{userId}
            if (method == "DELETE" && path.StartsWith("/api/users/") &&
                int.TryParse(path.Split("/")[3], out int deleteId))
            {
                var message = await _userController.DeleteUserAsync(deleteId, token);
                HttpResponseHelper.WriteSuccess(response, new { id = deleteId }, message, path);
                return true;
            }
            // PUT /api/users/{userId}/type-account
            if (method == "PUT" && path.Contains("/type-account") &&
                int.TryParse(path.Split("/")[3], out int typeAccId))
            {
                var payload = await ParseJson<Dictionary<string, string>>(request);
                if (!payload.TryGetValue("typeAccount", out string? newTypeAccount) || string.IsNullOrWhiteSpace(newTypeAccount))
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thi?u ho?c sai tham s? typeAccount", path);
                    return true;
                }
                var message = await _userController.UpdateUserTypeAccountAsync(typeAccId, newTypeAccount, token);
                HttpResponseHelper.WriteSuccess(response, new { message }, message, path);
                return true;
            }
            // GET /api/users/{userId}/type-account
            if (method == "GET" && path.Contains("/type-account") &&
                int.TryParse(path.Split("/")[3], out int getTypeId))
            {
                var result = await _userController.GetTypeAccountAsync(getTypeId, token);
                if (result == null)
                    HttpResponseHelper.WriteUnauthorized(response, "Kh�ng th? l?y lo?i t�i kho?n", path);
                else
                    HttpResponseHelper.WriteSuccess(response, new { typeAccount = result }, "L?y lo?i t�i kho?n th�nh c�ng", path);
                return true;
            }
            // GET /api/users/map-role?typeAccount={typeAccount}
            if (method == "GET" && path.StartsWith("/api/users/map-role") && request.Url?.Query != null)
            {
                var query = System.Web.HttpUtility.ParseQueryString(request.Url.Query);
                string? typeAccount = query.Get("typeAccount");
                if (string.IsNullOrEmpty(typeAccount))
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thi?u tham s? typeAccount", path);
                    return true;
                }
                var roleId = await _userController.MapTypeAccountToRoleIdAsync(typeAccount, token);
                HttpResponseHelper.WriteSuccess(response, new { roleId }, "�nh x? th�nh c�ng", path);
                return true;
            }
            HttpResponseHelper.WriteNotFound(response, "Kh�ng t�m th?y API y�u c?u", path);
            return true;
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }
    private static string? GetAccessToken(HttpListenerRequest request)
    {
        string? authHeader = request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;
        return authHeader["Bearer ".Length..].Trim();
    }
    private static async Task<T> ParseJson<T>(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body)!;
    }
}
