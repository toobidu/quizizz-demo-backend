using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Router;
public class SocketConnectionRouter : IBaseRouter
{
    private readonly SocketConnectionController _controller;
    public SocketConnectionRouter(SocketConnectionController controller)
    {
        _controller = controller;
    }
    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        if (!path.StartsWith("/api/socket-connections")) return false;
        try
        {
            switch ((method.ToUpper(), path))
            {
                case ("GET", "/api/socket-connections"):
                    await HandleGetById(request, response);
                    return true;
                case ("GET", "/api/socket-connections/by-socket-id"):
                    await HandleGetBySocketId(request, response);
                    return true;
                case ("GET", "/api/socket-connections/by-room"):
                    await HandleGetByRoomId(request, response);
                    return true;
                case ("GET", "/api/socket-connections/by-user"):
                    await HandleGetByUserId(request, response);
                    return true;
                case ("POST", "/api/socket-connections"):
                    await HandleCreate(request, response);
                    return true;
                case ("PUT", "/api/socket-connections"):
                    await HandleUpdate(request, response);
                    return true;
                case ("DELETE", "/api/socket-connections"):
                    await HandleDelete(request, response);
                    return true;
                case ("DELETE", "/api/socket-connections/by-socket-id"):
                    await HandleDeleteBySocketId(request, response);
                    return true;
                case ("PUT", "/api/socket-connections/activity"):
                    await HandleUpdateLastActivity(request, response);
                    return true;
                default:
                    HttpResponseHelper.WriteNotFound(response, "API không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }
    private async Task HandleGetById(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid socket connection ID", "/api/socket-connections");
            return;
        }
        var apiResponse = await _controller.GetByIdAsync(id);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleGetBySocketId(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        var socketId = queryParams["socketId"];
        if (string.IsNullOrEmpty(socketId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid socket ID", "/api/socket-connections/by-socket-id");
            return;
        }
        var apiResponse = await _controller.GetBySocketIdAsync(socketId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleGetByRoomId(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["roomId"], out var roomId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid room ID", "/api/socket-connections/by-room");
            return;
        }
        var apiResponse = await _controller.GetByRoomIdAsync(roomId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleGetByUserId(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["userId"], out var userId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid user ID", "/api/socket-connections/by-user");
            return;
        }
        var apiResponse = await _controller.GetByUserIdAsync(userId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleCreate(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        try
        {
            var socketConnection = JsonSerializer.Deserialize<SocketConnection>(body, JsonSerializerConfig.DefaultOptions);
            if (socketConnection == null || string.IsNullOrEmpty(socketConnection.SocketId))
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid socket connection data", "/api/socket-connections");
                return;
            }
            var apiResponse = await _controller.CreateAsync(socketConnection);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/socket-connections");
        }
    }
    private async Task HandleUpdate(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        try
        {
            var socketConnection = JsonSerializer.Deserialize<SocketConnection>(body, JsonSerializerConfig.DefaultOptions);
            if (socketConnection == null || string.IsNullOrEmpty(socketConnection.SocketId))
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid socket connection data", "/api/socket-connections");
                return;
            }
            var apiResponse = await _controller.UpdateAsync(socketConnection);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/socket-connections");
        }
    }
    private async Task HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid socket connection ID", "/api/socket-connections");
            return;
        }
        var apiResponse = await _controller.DeleteAsync(id);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleDeleteBySocketId(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        var socketId = queryParams["socketId"];
        if (string.IsNullOrEmpty(socketId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid socket ID", "/api/socket-connections/by-socket-id");
            return;
        }
        var apiResponse = await _controller.DeleteBySocketIdAsync(socketId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
    private async Task HandleUpdateLastActivity(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        var socketId = queryParams["socketId"];
        if (string.IsNullOrEmpty(socketId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid socket ID", "/api/socket-connections/activity");
            return;
        }
        var apiResponse = await _controller.UpdateLastActivityAsync(socketId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
}
