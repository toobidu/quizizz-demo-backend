using ConsoleApp1.Controller;
using ConsoleApp1.Config;
using ConsoleApp1.Router;
using System.Net;
using System.Text;
using System.Text.Json;

namespace ConsoleApp1.Router;

public class JoinRoomRouter : IBaseRouter
{
    private readonly JoinRoomController _controller;

    public JoinRoomRouter(JoinRoomController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/rooms")) return false;

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
                case "GET" when path == "/api/rooms/public":
                    await GetPublicRooms(response, path);
                    return true;
                case "POST" when path.StartsWith("/api/rooms/") && path.EndsWith("/join"):
                    await JoinPublicRoom(response, path);
                    return true;
                case "POST" when path == "/api/rooms/join-private":
                    await JoinPrivateRoom(request, response, path);
                    return true;
                case "DELETE" when path.StartsWith("/api/rooms/") && path.EndsWith("/leave"):
                    await LeaveRoom(response, path);
                    return true;
                case "GET" when path.StartsWith("/api/rooms/code/"):
                    await GetRoomByCode(response, path);
                    return true;
                case "GET" when path.StartsWith("/api/rooms/") && path.EndsWith("/players"):
                    await GetPlayersInRoom(response, path);
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

    private async Task GetPublicRooms(HttpListenerResponse response, string path)
    {
        var result = await _controller.GetPublicRoomsAsync(1);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task JoinPublicRoom(HttpListenerResponse response, string path)
    {
        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var result = await _controller.JoinPublicRoomAsync(roomId, 1);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task JoinPrivateRoom(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var requestData = await ParseJson<Dictionary<string, string>>(request);
        if (requestData == null || !requestData.ContainsKey("roomCode"))
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }

        var result = await _controller.JoinPrivateRoomAsync(requestData["roomCode"], 1);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task LeaveRoom(HttpListenerResponse response, string path)
    {
        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var result = await _controller.LeaveRoomAsync(roomId, 1);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task GetRoomByCode(HttpListenerResponse response, string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 5)
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }

        var roomCode = parts[4];
        var result = await _controller.GetRoomByCodeAsync(roomCode, 1);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task GetPlayersInRoom(HttpListenerResponse response, string path)
    {
        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }

        var result = await _controller.GetPlayersInRoomAsync(roomId, 1);
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
        return JsonSerializer.Deserialize<T>(body);
    }
}