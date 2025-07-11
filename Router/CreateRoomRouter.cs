using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Router;
using ConsoleApp1.Config;
using ConsoleApp1.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net;
using System.Text;

namespace ConsoleApp1.Router;

public class CreateRoomRouter : IBaseRouter
{
    private readonly CreateRoomController _controller;
    private readonly JwtHelper _jwtHelper;

    public CreateRoomRouter(CreateRoomController controller, JwtHelper jwtHelper)
    {
        _controller = controller;
        _jwtHelper = jwtHelper;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/rooms")) return false;
        
        Console.WriteLine($"[CREATE_ROOM_ROUTER] Handling request: {method} {path}");

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
                case "POST" when path == "/api/rooms/create":
                    await CreateRoom(request, response, token, path);
                    return true;
                case "PUT" when path.StartsWith("/api/rooms/") && path.EndsWith("/settings"):
                    await UpdateSettings(request, response, path, token);
                    return true;
                case "DELETE" when path.StartsWith("/api/rooms/") && path.Contains("/players/"):
                    await KickPlayer(response, path, token);
                    return true;
                case "PUT" when path.StartsWith("/api/rooms/") && path.EndsWith("/status"):
                    await UpdateStatus(request, response, path, token);
                    return true;
                case "DELETE" when path.StartsWith("/api/rooms/") && !path.Contains("/players/"):
                    await DeleteRoom(response, path, token);
                    return true;
                case "PUT" when path.StartsWith("/api/rooms/") && path.EndsWith("/transfer"):
                    await TransferOwnership(request, response, path, token);
                    return true;
                default:
                    HttpResponseHelper.WriteNotFound(response, "Endpoint không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task CreateRoom(HttpListenerRequest request, HttpListenerResponse response, string token, string path)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var dto = await ParseJson<CreateRoomRequest>(request);
        if (dto == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Dữ liệu không hợp lệ", path);
            return;
        }

        var result = await _controller.CreateRoomAsync(dto, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task UpdateSettings(HttpListenerRequest request, HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var settings = await ParseJson<RoomSetting>(request);
        if (settings == null)
        {
            HttpResponseHelper.WriteBadRequest(response, "Dữ liệu không hợp lệ", path);
            return;
        }

        var result = await _controller.UpdateRoomSettingsAsync(roomId, settings, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task KickPlayer(HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var parts = path.Split('/');
        if (parts.Length < 6 || !int.TryParse(parts[3], out int roomId) || !int.TryParse(parts[5], out int playerId))
        {
            HttpResponseHelper.WriteBadRequest(response, "URL hoặc ID không hợp lệ", path);
            return;
        }

        var result = await _controller.KickPlayerAsync(roomId, playerId, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task UpdateStatus(HttpListenerRequest request, HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var statusRequest = await ParseJson<Dictionary<string, string>>(request);
        if (statusRequest == null || !statusRequest.ContainsKey("status"))
        {
            HttpResponseHelper.WriteBadRequest(response, "Trạng thái không hợp lệ", path);
            return;
        }

        var result = await _controller.UpdateRoomStatusAsync(roomId, statusRequest["status"], userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task DeleteRoom(HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var result = await _controller.DeleteRoomAsync(roomId, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task TransferOwnership(HttpListenerRequest request, HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }

        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var transferRequest = await ParseJson<Dictionary<string, int>>(request);
        if (transferRequest == null || !transferRequest.ContainsKey("newOwnerId"))
        {
            HttpResponseHelper.WriteBadRequest(response, "ID chủ mới không hợp lệ", path);
            return;
        }

        var result = await _controller.TransferOwnershipAsync(roomId, transferRequest["newOwnerId"], userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private int ExtractRoomId(string endpoint)
    {
        var parts = endpoint.Split('/');
        return parts.Length > 3 && int.TryParse(parts[3], out int roomId) ? roomId : 0;
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
        Console.WriteLine($"[CREATE_ROOM_ROUTER] Request body: {body}");
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = JsonSerializer.Deserialize<T>(body, options);
        Console.WriteLine($"[CREATE_ROOM_ROUTER] Deserialized: {JsonSerializer.Serialize(result, options)}");
        return result;
    }
}