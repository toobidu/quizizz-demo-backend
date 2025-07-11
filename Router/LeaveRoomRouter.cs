using System.Net;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Security;

namespace ConsoleApp1.Router;

public class LeaveRoomRouter : IBaseRouter
{
    private readonly LeaveRoomController _controller;
    private readonly JwtHelper _jwtHelper;

    public LeaveRoomRouter(LeaveRoomController controller, JwtHelper jwtHelper)
    {
        _controller = controller;
        _jwtHelper = jwtHelper;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/rooms/") || !path.EndsWith("/leave") || method != "POST") 
            return false;

        Console.WriteLine($"[LEAVE_ROOM_ROUTER] Handling request: {method} {path}");

        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
            return true;
        }

        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return true;
        }

        try
        {
            var roomId = ExtractRoomId(path);
            if (roomId == 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
                return true;
            }

            var result = await _controller.LeaveRoomAsync(roomId, userId.Value);
            HttpResponseHelper.WriteJsonResponse(response, result);
            return true;
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
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
}