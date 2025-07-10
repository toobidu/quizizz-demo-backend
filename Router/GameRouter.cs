using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

public class GameRouter : IBaseRouter
{
    private readonly GameController _controller;

    public GameRouter(GameController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/game")) return false;

        try
        {
            switch ((method.ToUpper(), path))
            {
                case ("POST", "/api/game/start"):
                    await HandleStartGame(request, response);
                    return true;

                case ("POST", "/api/game/question"):
                    await HandleSendQuestion(request, response);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "API không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME_ROUTER] Exception: {ex.Message}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task HandleStartGame(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        var data = JsonSerializer.Deserialize<dynamic>(body);
        // Extract roomCode and hostUserId from request
        
        var apiResponse = await _controller.StartGameAsync("ROOM123", 1); // Placeholder
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleSendQuestion(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        var data = JsonSerializer.Deserialize<dynamic>(body);
        
        var apiResponse = await _controller.SendQuestionAsync("ROOM123", data, 1, 10); // Placeholder with required params
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }
}