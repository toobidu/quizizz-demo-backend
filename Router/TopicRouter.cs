using System.Net;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

public class TopicRouter : IBaseRouter
{
    private readonly TopicController _controller;

    public TopicRouter(TopicController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        if (!path.StartsWith("/api/topics")) return false;

        Console.WriteLine($"[TOPIC_ROUTER] Handling request: {method} {path}");

        try
        {
            switch (method.ToUpper())
            {
                case "GET" when path == "/api/topics":
                    await GetAllTopics(response);
                    return true;
                case "GET" when path.StartsWith("/api/topics/"):
                    await GetTopicById(response, path);
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

    private async Task GetAllTopics(HttpListenerResponse response)
    {
        var result = await _controller.GetAllTopicsAsync();
        HttpResponseHelper.WriteJsonResponse(response, result);
    }

    private async Task GetTopicById(HttpListenerResponse response, string path)
    {
        var parts = path.Split('/');
        if (parts.Length < 4 || !int.TryParse(parts[3], out int topicId))
        {
            HttpResponseHelper.WriteBadRequest(response, "ID chủ đề không hợp lệ", path);
            return;
        }

        var result = await _controller.GetTopicByIdAsync(topicId);
        HttpResponseHelper.WriteJsonResponse(response, result);
    }
}