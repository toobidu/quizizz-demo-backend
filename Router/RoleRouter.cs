using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Users;
namespace ConsoleApp1.Router;
public class RoleRouter : IBaseRouter
{
    private readonly RoleController _roleController;
    public RoleRouter(RoleController roleController)
    {
        _roleController = roleController;
    }
    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        if (!path.StartsWith("/api/roles")) return false;
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thi?u ho?c sai th�ng tin x�c th?c", path);
            return true;
        }
        try
        {
            if (method == "GET" && path == "/api/roles")
            {
                var result = await _roleController.GetAllRolesAsync(token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y danh s�ch vai tr� th�nh c�ng", path);
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/roles/exists/"))
            {
                var segments = path.Split("/");
                if (segments.Length < 5)
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thi?u roleName", path);
                    return true;
                }
                string roleName = segments[4];
                var result = await _roleController.RoleNameExistsAsync(roleName, token);
                HttpResponseHelper.WriteSuccess(response, new { exists = result }, "Ki?m tra vai tr� t?n t?i", path);
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/roles/permission/") &&
                int.TryParse(path.Split("/")[4], out int permissionId))
            {
                var result = await _roleController.GetRolesByPermissionIdAsync(permissionId, token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y vai tr� theo quy?n th�nh c�ng", path);
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/roles/user/") &&
                int.TryParse(path.Split("/")[4], out int userId))
            {
                var result = await _roleController.GetRolesByUserIdAsync(userId, token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y vai tr� theo ngu?i d�ng th�nh c�ng", path);
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/roles/") &&
                int.TryParse(path.Split("/")[3], out int id))
            {
                var result = await _roleController.GetRoleByIdAsync(id, token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y chi ti?t vai tr� th�nh c�ng", path);
                return true;
            }
            if (method == "POST" && path == "/api/roles")
            {
                var dto = await ParseJson<RoleDTO>(request);
                var result = await _roleController.CreateRoleAsync(dto, token);
                HttpResponseHelper.WriteSuccess(response, result, "T?o vai tr� m?i th�nh c�ng", path);
                return true;
            }
            if (method == "PUT" && path == "/api/roles")
            {
                var dto = await ParseJson<RoleDTO>(request);
                var result = await _roleController.UpdateRoleAsync(dto, token);
                HttpResponseHelper.WriteSuccess(response, result, "C?p nh?t vai tr� th�nh c�ng", path);
                return true;
            }
            if (method == "DELETE" && path.StartsWith("/api/roles/") &&
                int.TryParse(path.Split("/")[3], out int deleteId))
            {
                var message = await _roleController.DeleteRoleAsync(deleteId, token);
                HttpResponseHelper.WriteSuccess(response, new { id = deleteId }, message, path);
                return true;
            }
            if (method == "POST" && path.StartsWith("/api/roles/user/") && path.EndsWith("/permissions"))
            {
                var segments = path.Split("/");
                if (segments.Length < 5 || !int.TryParse(segments[4], out int uid))
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thi?u ho?c sai userId", path);
                    return true;
                }
                var permissionNames = await ParseJson<List<string>>(request);
                var result = await _roleController.GetRolesByUserIdAndPermissionNamesAsync(uid, permissionNames, token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y vai tr� theo quy?n v� ngu?i d�ng", path);
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
