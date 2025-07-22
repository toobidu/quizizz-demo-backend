using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Users;
namespace ConsoleApp1.Router;
public class PermissionRouter : IBaseRouter
{
    private readonly PermissionController _permissionController;
    public PermissionRouter(PermissionController permissionController)
    {
        _permissionController = permissionController;
    }
    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        if (!path.StartsWith("/api/permissions")) return false;
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thi?u ho?c sai thông tin xác th?c", path);
            return true;
        }
        try
        {
            if (method == "GET" && path == "/api/permissions")
            {
                var result = await _permissionController.GetAllPermissionsAsync(token);
                HttpResponseHelper.WriteSuccess(response, result, "L?y danh sách quy?n thành công", path);
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/permissions/") &&
                int.TryParse(path.Split("/")[3], out int id))
            {
                var result = await _permissionController.GetPermissionByIdAsync(id, token);
                if (result != null)
                {
                    HttpResponseHelper.WriteSuccess(response, result, "L?y chi ti?t quy?n thành công", path);
                }
                else
                {
                    HttpResponseHelper.WriteNotFound(response, "Không tìm th?y quy?n", path);
                }
                return true;
            }
            if (method == "GET" && path.StartsWith("/api/permissions/exists/"))
            {
                var segments = path.Split("/");
                if (segments.Length < 5)
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thi?u permission name", path);
                    return true;
                }
                string permissionName = segments[4];
                var exists = await _permissionController.PermissionNameExistsAsync(permissionName, token);
                HttpResponseHelper.WriteSuccess(response, new { exists }, "Ki?m tra quy?n t?n t?i", path);
                return true;
            }
            if (method == "POST" && path == "/api/permissions")
            {
                var dto = await ParseJson<PermissionDTO>(request);
                var message = await _permissionController.CreatePermissionAsync(dto, token);
                HttpResponseHelper.WriteSuccess(response, message, "T?o quy?n", path);
                return true;
            }
            if (method == "PUT" && path == "/api/permissions")
            {
                var dto = await ParseJson<PermissionDTO>(request);
                var message = await _permissionController.UpdatePermissionAsync(dto, token);
                HttpResponseHelper.WriteSuccess(response, message, "C?p nh?t quy?n", path);
                return true;
            }
            if (method == "DELETE" && path.StartsWith("/api/permissions/") &&
                int.TryParse(path.Split("/")[3], out int deleteId))
            {
                var message = await _permissionController.DeletePermissionAsync(deleteId, token);
                HttpResponseHelper.WriteSuccess(response, new { id = deleteId }, message, path);
                return true;
            }
            HttpResponseHelper.WriteNotFound(response, "Không tìm th?y API yêu c?u", path);
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
