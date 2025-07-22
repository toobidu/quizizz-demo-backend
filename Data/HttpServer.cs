using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Router;
public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly List<IBaseRouter> _routers;
    public HttpServer(string urlPrefix, params IBaseRouter[] routers)
    {
        _listener.Prefixes.Add(urlPrefix);
        _routers = routers.ToList();
    }
    public async Task StartAsync()
    {
        _listener.Start();
        while (true)
        {
            var context = await _listener.GetContextAsync();
            _ = Task.Run(() => HandleRequestAsync(context));
        }
    }
    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        // Set CORS headers for all requests
        SetCorsHeaders(response);
        string path = request.Url?.AbsolutePath ?? "unknown";
        string method = request.HttpMethod;
        string clientIP = request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        // Handle OPTIONS preflight requests
        if (method == "OPTIONS")
        {
            response.StatusCode = 200;
            response.OutputStream.Close();
            return;
        }
        try
        {
            bool routeFound = false;
            foreach (var router in _routers)
            {
                bool handled = await router.HandleAsync(request, response);
                if (handled)
                {
                    routeFound = true;
                    response.OutputStream.Close();
                    return;
                }
            }
            if (!routeFound)
            {
                response.StatusCode = 404;
                await WriteJson(response, new { error = "404 Not Found", path = path });
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteJson(response, new { error = "Internal Server Error", detail = ex.Message });
        }
        response.OutputStream.Close();
    }
    private static async Task WriteJson(HttpListenerResponse res, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var buffer = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json";
        res.ContentEncoding = Encoding.UTF8;
        res.ContentLength64 = buffer.Length;
        await res.OutputStream.WriteAsync(buffer);
    }
    private static void SetCorsHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}
