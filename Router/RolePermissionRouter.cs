using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
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
                HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
                return true;
            }

            if (method == "POST" && path == "/api/role-permission/assign")
            {
                if (!TryParseIntQuery(request, "roleId", out var roleId) || !TryParseIntQuery(request, "permissionId", out var permissionId))
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thiếu roleId hoặc permissionId", path);
                    return true;
                }

                var result = await _controller.AssignPermissionToRoleApi(roleId, permissionId, token);
                HttpResponseHelper.WriteSuccess(response, result, "Gán quyền thành công", path);
                return true;
            }

            if (method == "DELETE" && path == "/api/role-permission/remove")
            {
                if (!TryParseIntQuery(request, "roleId", out var roleId) || !TryParseIntQuery(request, "permissionId", out var permissionId))
                {
                    HttpResponseHelper.WriteBadRequest(response, "Thiếu roleId hoặc permissionId", path);
                    return true;
                }

                var result = await _controller.RemovePermissionFromRoleApi(roleId, permissionId, token);
                HttpResponseHelper.WriteSuccess(response, result, "Xóa quyền khỏi vai trò thành công", path);
                return true;
            }

            if (method == "POST" && path == "/api/role-permission/assign-multiple")
            {
                var jsonDoc = await ParseJson(request);
                int roleId = jsonDoc.RootElement.GetProperty("roleId").GetInt32();
                var permissionIds = jsonDoc.RootElement.GetProperty("permissionIds")
                    .EnumerateArray().Select(p => p.GetInt32()).ToList();

                var result = await _controller.AssignMultiplePermissionsToRoleApi(roleId, permissionIds, token);
                HttpResponseHelper.WriteSuccess(response, result, "Gán nhiều quyền thành công", path);
                return true;
            }

            if (method == "DELETE" && path == "/api/role-permission/remove-multiple")
            {
                var jsonDoc = await ParseJson(request);
                int roleId = jsonDoc.RootElement.GetProperty("roleId").GetInt32();
                var permissionIds = jsonDoc.RootElement.GetProperty("permissionIds")
                    .EnumerateArray().Select(p => p.GetInt32()).ToList();

                var result = await _controller.RemoveMultiplePermissionsFromRoleApi(roleId, permissionIds, token);
                HttpResponseHelper.WriteSuccess(response, result, "Xóa nhiều quyền khỏi vai trò thành công", path);
                return true;
            }

            HttpResponseHelper.WriteNotFound(response, "Không tìm thấy API yêu cầu", path);
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

    private static async Task<JsonDocument> ParseJson(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        return JsonDocument.Parse(await reader.ReadToEndAsync());
    }

    private static bool TryParseIntQuery(HttpListenerRequest request, string key, out int value)
    {
        value = 0;
        var valStr = request.QueryString[key];
        return valStr != null && int.TryParse(valStr, out value);
    }
}
