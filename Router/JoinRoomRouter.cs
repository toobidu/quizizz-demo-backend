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
    private readonly Security.JwtHelper _jwtHelper;
    public JoinRoomRouter(JoinRoomController controller, Security.JwtHelper jwtHelper)
    {
        _controller = controller;
        _jwtHelper = jwtHelper;
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
                    await GetPublicRooms(request, response, path);
                    return true;
                case "GET" when path == "/api/rooms/all":
                    await GetAllRooms(request, response, path);
                    return true;
                case "POST" when path.StartsWith("/api/rooms/") && path.EndsWith("/join"):
                    await JoinRoomByCode(request, response, path, token);
                    return true;
                case "POST" when path == "/api/rooms/join-private":
                    await JoinPrivateRoom(request, response, path);
                    return true;
                case "POST" when path == "/api/rooms/join-public":
                    await JoinPublicRoom(request, response, path);
                    return true;
                case "DELETE" when path.StartsWith("/api/rooms/") && path.EndsWith("/leave"):
                    await LeaveRoom(request, response, path);
                    return true;
                case "POST" when path.StartsWith("/api/rooms/") && path.EndsWith("/leave"):
                    await LeaveRoomByCode(request, response, path);
                    return true;
                case "GET" when path.StartsWith("/api/rooms/code/"):
                    await GetRoomByCode(request, response, path);
                    return true;
                case "GET" when path.StartsWith("/api/rooms/") && path.EndsWith("/players"):
                    await GetPlayersInRoom(request, response, path);
                    return true;
                case "GET" when path.StartsWith("/api/rooms/") && path.EndsWith("/details"):
                    await GetRoomDetails(request, response, path);
                    return true;
                case "POST" when path.StartsWith("/api/rooms/") && path.EndsWith("/start"):
                    await StartGame(request, response, path);
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
    private async Task GetPublicRooms(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.GetPublicRoomsAsync(userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task GetAllRooms(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu hoặc sai thông tin xác thực", path);
            return;
        }
        int? userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.GetAllRoomsAsync(userId.Value);
        var dataCount = result.Data?.Count() ?? 0;
        var success = result.Message;
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task JoinRoomByCode(HttpListenerRequest request, HttpListenerResponse response, string path, string token)
    {
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var roomCode = ExtractRoomCode(path);
        if (string.IsNullOrEmpty(roomCode))
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }
        var result = await _controller.JoinPrivateRoomAsync(roomCode, userId.Value);
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
        var userId = _jwtHelper.GetUserIdFromToken(GetAccessToken(request));
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.JoinPrivateRoomAsync(requestData["roomCode"], userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task JoinPublicRoom(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var requestData = await ParseJson<Dictionary<string, string>>(request);
        if (requestData == null || !requestData.ContainsKey("roomCode"))
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(GetAccessToken(request));
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.JoinPrivateRoomAsync(requestData["roomCode"], userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task LeaveRoom(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var roomId = ExtractRoomId(path);
        if (roomId == 0)
        {
            HttpResponseHelper.WriteBadRequest(response, "ID phòng không hợp lệ", path);
            return;
        }
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.LeaveRoomAsync(roomId, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task GetRoomByCode(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 5)
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }
        var roomCode = parts[4];
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.GetRoomByCodeAsync(roomCode, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task GetPlayersInRoom(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        // Thử parse roomId trước
        var roomId = ExtractRoomId(path);
        if (roomId > 0)
        {
            var result = await _controller.GetPlayersInRoomAsync(roomId, userId.Value);
            // Sau khi lấy dữ liệu, gọi broadcast để đảm bảo cập nhật qua WebSocket
            try
            {
                var room = await _controller.GetRoomDetailsAsync(roomId, userId.Value);
                if (room.IsSuccess && room.Data != null)
                {
                    // Gọi broadcast để cập nhật danh sách người chơi qua WebSocket
                    await _controller._joinRoomService.BroadcastRoomPlayersUpdateAsync(room.Data.RoomCode);
                }
            }
            catch (Exception ex)
            {
            }
            // Trả về kết quả API
            HttpResponseHelper.WriteJsonResponse(response, result);
            return;
        }
        // Nếu không parse được roomId, thử roomCode
        var roomCode = ExtractRoomCode(path);
        if (!string.IsNullOrEmpty(roomCode))
        {
            // Lấy roomId từ roomCode
            var roomResult = await _controller.GetRoomByCodeAsync(roomCode, userId.Value);
            if (roomResult.IsSuccess && roomResult.Data != null)
            {
                var actualRoomId = roomResult.Data.Id;
                var result = await _controller.GetPlayersInRoomAsync(actualRoomId, userId.Value);
                // Gọi broadcast để cập nhật danh sách người chơi qua WebSocket
                try
                {
                    await _controller._joinRoomService.BroadcastRoomPlayersUpdateAsync(roomCode);
                }
                catch (Exception ex)
                {
                }
                HttpResponseHelper.WriteJsonResponse(response, result);
                return;
            }
        }
        HttpResponseHelper.WriteBadRequest(response, "ID hoặc mã phòng không hợp lệ", path);
    }
    private string ExtractRoomCode(string endpoint)
    {
        var parts = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // URL pattern: /api/rooms/{roomCode}/leave or /api/rooms/{roomCode}/join
        if (parts.Length >= 3 && parts[0] == "api" && parts[1] == "rooms")
        {
            var roomCode = parts[2];
            return roomCode;
        }
        return string.Empty;
    }
    private int ExtractRoomId(string endpoint)
    {
        var parts = endpoint.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // URL pattern: /api/rooms/{roomId}/players or /api/rooms/{roomId}/details
        if (parts.Length >= 3 && parts[0] == "api" && parts[1] == "rooms")
        {
            if (int.TryParse(parts[2], out int roomId))
            {
                return roomId;
            }
            else
            {
            }
        }
        else
        {
        }
        return 0;
    }
    private static string? GetAccessToken(HttpListenerRequest request)
    {
        string? authHeader = request.Headers["Authorization"];
        if (authHeader == null || !authHeader.StartsWith("Bearer ")) return null;
        return authHeader["Bearer ".Length..].Trim();
    }
    private async Task GetRoomDetails(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        int? userId = _jwtHelper.GetUserIdFromToken(token);
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
        var result = await _controller.GetRoomDetailsAsync(roomId, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task StartGame(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        int? userId = _jwtHelper.GetUserIdFromToken(token);
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
        var result = await _controller.StartGameAsync(roomId, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private async Task LeaveRoomByCode(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        var roomCode = ExtractRoomCode(path);
        if (string.IsNullOrEmpty(roomCode))
        {
            HttpResponseHelper.WriteBadRequest(response, "Mã phòng không hợp lệ", path);
            return;
        }
        string? token = GetAccessToken(request);
        if (token == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Thiếu token", path);
            return;
        }
        var userId = _jwtHelper.GetUserIdFromToken(token);
        if (userId == null)
        {
            HttpResponseHelper.WriteUnauthorized(response, "Token không hợp lệ", path);
            return;
        }
        var result = await _controller.LeaveRoomByCodeAsync(roomCode, userId.Value);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
    private static async Task<T?> ParseJson<T>(HttpListenerRequest req)
    {
        using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<T>(body);
    }
}
