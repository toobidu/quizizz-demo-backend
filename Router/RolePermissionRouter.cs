using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

public class RolePermissionRouter : IBaseRouter
{
    private readonly RolePermissionController _controller;

    public RolePermissionRouter(RolePermissionController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/role-permission")) return false;

        try
        {
            string? token = GetAccessToken(request);
            if (token == null)
            {
                response.StatusCode = 401;
                await WriteJson(response, new { error = "Thiếu hoặc sai Authorization header" });
                return true;
            }

            if (method == "POST" && path == "/api/role-permission/assign")
            {
                int roleId = int.Parse(request.QueryString["roleId"]!);
                int permissionId = int.Parse(request.QueryString["permissionId"]!);
                var result = await _controller.AssignPermissionToRoleApi(roleId, permissionId, token);
                await WriteJson(response, new { message = result });
                return true;
            }
            else if (method == "DELETE" && path == "/api/role-permission/remove")
            {
                int roleId = int.Parse(request.QueryString["roleId"]!);
                int permissionId = int.Parse(request.QueryString["permissionId"]!);
                var result = await _controller.RemovePermissionFromRoleApi( roleId, permissionId, token);
                await WriteJson(response, new { message = result });
                return true;
            }
            else if (method == "POST" && path == "/api/role-permission/assign-multiple")
            {
                var jsonDoc = await ParseJson(request);
                int roleId = jsonDoc.RootElement.GetProperty("roleId").GetInt32();
                var permissionIds = jsonDoc.RootElement.GetProperty("permissionIds").EnumerateArray().Select(p => p.GetInt32()).ToList();
                var result = await _controller.AssignMultiplePermissionsToRoleApi(roleId, permissionIds, token);
                await WriteJson(response, new { message = result });
                return true;
            }
            else if (method == "DELETE" && path == "/api/role-permission/remove-multiple")
            {
                var jsonDoc = await ParseJson(request);
                int roleId = jsonDoc.RootElement.GetProperty("roleId").GetInt32();
                var permissionIds = jsonDoc.RootElement.GetProperty("permissionIds").EnumerateArray().Select(p => p.GetInt32()).ToList();
                var result = await _controller.RemoveMultiplePermissionsFromRoleApi(roleId, permissionIds, token);
                await WriteJson(response, new { message = result });
                return true;
            }
            else
            {
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

    private static string? GetAccessToken(HttpListenerRequest request)
    {
        string? authHeader = request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;
        return authHeader.Substring("Bearer ".Length).Trim();
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
