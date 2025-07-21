using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Router;

public class GameSessionRouter : IBaseRouter
{
    private readonly GameSessionController _controller;

    public GameSessionRouter(GameSessionController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/game-sessions")) return false;

        try
        {
            switch ((method.ToUpper(), path))
            {
                case ("GET", "/api/game-sessions"):
                    await HandleGetById(request, response);
                    return true;

                case ("GET", "/api/game-sessions/by-room"):
                    await HandleGetByRoomId(request, response);
                    return true;

                case ("POST", "/api/game-sessions"):
                    await HandleCreate(request, response);
                    return true;

                case ("PUT", "/api/game-sessions"):
                    await HandleUpdate(request, response);
                    return true;

                case ("DELETE", "/api/game-sessions"):
                    await HandleDelete(request, response);
                    return true;

                case ("PUT", "/api/game-sessions/state"):
                    await HandleUpdateGameState(request, response);
                    return true;

                case ("PUT", "/api/game-sessions/question-index"):
                    await HandleUpdateQuestionIndex(request, response);
                    return true;

                case ("POST", "/api/game-sessions/end"):
                    await HandleEndGameSession(request, response);
                    return true;

                case ("GET", "/api/game-sessions/questions"):
                    await HandleGetGameQuestions(request, response);
                    return true;

                case ("POST", "/api/game-sessions/questions"):
                    await HandleAddQuestionsToGameSession(request, response);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "API không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME_SESSION_ROUTER] Exception: {ex.Message}");
            HttpResponseHelper.WriteInternalServerError(response, ex.Message, path);
            return true;
        }
    }

    private async Task HandleGetById(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions");
            return;
        }

        var apiResponse = await _controller.GetByIdAsync(id);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleGetByRoomId(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["roomId"], out var roomId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid room ID", "/api/game-sessions/by-room");
            return;
        }

        var apiResponse = await _controller.GetByRoomIdAsync(roomId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleCreate(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        try
        {
            var gameSession = JsonSerializer.Deserialize<GameSession>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (gameSession == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid game session data", "/api/game-sessions");
                return;
            }

            var apiResponse = await _controller.CreateAsync(gameSession);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game-sessions");
        }
    }

    private async Task HandleUpdate(HttpListenerRequest request, HttpListenerResponse response)
    {
        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        try
        {
            var gameSession = JsonSerializer.Deserialize<GameSession>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (gameSession == null || gameSession.Id <= 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid game session data", "/api/game-sessions");
                return;
            }

            var apiResponse = await _controller.UpdateAsync(gameSession);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game-sessions");
        }
    }

    private async Task HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions");
            return;
        }

        var apiResponse = await _controller.DeleteAsync(id);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleUpdateGameState(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions/state");
            return;
        }

        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        try
        {
            var data = JsonSerializer.Deserialize<UpdateGameStateRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null || string.IsNullOrEmpty(data.GameState))
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid game state data", "/api/game-sessions/state");
                return;
            }

            var apiResponse = await _controller.UpdateGameStateAsync(id, data.GameState);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game-sessions/state");
        }
    }

    private async Task HandleUpdateQuestionIndex(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions/question-index");
            return;
        }

        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        try
        {
            var data = JsonSerializer.Deserialize<UpdateQuestionIndexRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid question index data", "/api/game-sessions/question-index");
                return;
            }

            var apiResponse = await _controller.UpdateCurrentQuestionIndexAsync(id, data.QuestionIndex);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game-sessions/question-index");
        }
    }

    private async Task HandleEndGameSession(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["id"], out var id))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions/end");
            return;
        }

        var apiResponse = await _controller.EndGameSessionAsync(id);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleGetGameQuestions(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["gameSessionId"], out var gameSessionId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions/questions");
            return;
        }

        var apiResponse = await _controller.GetGameQuestionsAsync(gameSessionId);
        HttpResponseHelper.WriteJsonResponse(response, apiResponse);
    }

    private async Task HandleAddQuestionsToGameSession(HttpListenerRequest request, HttpListenerResponse response)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url.Query);
        if (!int.TryParse(queryParams["gameSessionId"], out var gameSessionId))
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid game session ID", "/api/game-sessions/questions");
            return;
        }

        using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
        string body = await reader.ReadToEndAsync();
        
        try
        {
            var data = JsonSerializer.Deserialize<AddQuestionsRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (data == null || data.QuestionIds == null || !data.QuestionIds.Any())
            {
                HttpResponseHelper.WriteBadRequest(response, "Invalid question data", "/api/game-sessions/questions");
                return;
            }

            var apiResponse = await _controller.AddQuestionsToGameSessionAsync(gameSessionId, data.QuestionIds, data.TimeLimit);
            HttpResponseHelper.WriteJsonResponse(response, apiResponse);
        }
        catch (JsonException)
        {
            HttpResponseHelper.WriteBadRequest(response, "Invalid JSON format", "/api/game-sessions/questions");
        }
    }
}

public class UpdateGameStateRequest
{
    public string GameState { get; set; }
}

public class UpdateQuestionIndexRequest
{
    public int QuestionIndex { get; set; }
}

public class AddQuestionsRequest
{
    public List<int> QuestionIds { get; set; }
    public int TimeLimit { get; set; }
}

// Helper class for parsing query parameters
public static class HttpUtility
{
    public static System.Collections.Specialized.NameValueCollection ParseQueryString(string query)
    {
        var result = new System.Collections.Specialized.NameValueCollection();
        if (string.IsNullOrEmpty(query))
            return result;

        if (query.StartsWith("?"))
            query = query.Substring(1);

        string[] pairs = query.Split('&');
        foreach (string pair in pairs)
        {
            string[] parts = pair.Split('=');
            if (parts.Length == 2)
            {
                result.Add(parts[0], Uri.UnescapeDataString(parts[1]));
            }
            else if (parts.Length == 1)
            {
                result.Add(parts[0], "");
            }
        }

        return result;
    }
}