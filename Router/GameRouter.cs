using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;
public class StartGameRequest
{
    public string RoomCode { get; set; } = string.Empty;
    public int HostUserId { get; set; }
    public List<int> SelectedTopicIds { get; set; } = new();
    public int QuestionCount { get; set; } = 10;
    public int TimeLimit { get; set; } = 30;
}
public class GameRouter : IBaseRouter
{
    private readonly GameController _controller;
    private readonly QuestionController _questionController;

    public GameRouter(GameController controller, QuestionController questionController)
    {
        _controller = controller;
        _questionController = questionController;
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

                case var (m, p) when m == "GET" && p.StartsWith("/api/game/questions/"):
                    await HandleGetQuestions(request, response);
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
    private async Task HandleStartGame(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
            string body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                HttpResponseHelper.WriteBadRequest(response, "Request body is empty", "/api/game/start");
                return;
            }
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            };
            var requestData = JsonSerializer.Deserialize<StartGameRequest>(body, options);
            if (requestData == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game/start");
                return;
            }
            if (string.IsNullOrWhiteSpace(requestData.RoomCode))
            {
                HttpResponseHelper.WriteBadRequest(response, "RoomCode is required", "/api/game/start");
                return;
            }
            if (requestData.HostUserId <= 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "Valid HostUserId is required", "/api/game/start");
                return;
            }
            var apiResponse = await _controller.StartGameAsync(requestData.RoomCode, requestData.HostUserId);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException ex)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format: " + ex.Message, "/api/game/start");
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, "Server error: " + ex.Message, "/api/game/start");
        }
    }
    private async Task HandleSendQuestion(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        var data = JsonSerializer.Deserialize<dynamic>(body);
        var apiResponse = await _controller.SendQuestionAsync("ROOM123", data, 1, 10); // Placeholder with required params
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleGetQuestions(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            string path = request.Url?.AbsolutePath ?? "";
            
            // Extract room code from path: /api/game/questions/{roomCode}
            var pathParts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (pathParts.Length < 4)
            {
                HttpResponseHelper.WriteBadRequest(response, "Room code is required in path", path);
                return;
            }

            string roomCode = pathParts[3]; // api/game/questions/{roomCode}

            if (string.IsNullOrWhiteSpace(roomCode))
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid room code", path);
                return;
            }

            var apiResponse = await _questionController.GetQuestionsForRoomAsync(roomCode);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, "Server error: " + ex.Message, request.Url?.AbsolutePath ?? "");
        }
    }
}
