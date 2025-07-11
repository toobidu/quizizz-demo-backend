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
        Console.WriteLine($"🟢 HTTP server started on: {_listener.Prefixes.First()}");

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
        
        string path = request.Url?.AbsolutePath ?? "unknown";
        string method = request.HttpMethod;
        string clientIP = request.RemoteEndPoint?.Address?.ToString() ?? "unknown";
        
        Console.WriteLine($"[HttpServer] Incoming request: {method} {path} from {clientIP}");
        Console.WriteLine($"[HttpServer] Headers: {string.Join(", ", request.Headers.AllKeys.Select(k => $"{k}={request.Headers[k]}"))}");

        try
        {
            bool routeFound = false;
            foreach (var router in _routers)
            {
                bool handled = await router.HandleAsync(request, response);
                if (handled)
                {
                    Console.WriteLine($"[HttpServer] Request {method} {path} handled by {router.GetType().Name}");
                    routeFound = true;
                    response.OutputStream.Close();
                    return;
                }
            }

            if (!routeFound)
            {
                Console.WriteLine($"[HttpServer] No router found for {method} {path}");
                SetCorsHeaders(response);
                response.StatusCode = 404;
                await WriteJson(response, new { error = "404 Not Found", path = path });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HttpServer] Error handling {method} {path}: {ex.Message}");
            Console.WriteLine($"[HttpServer] Stack trace: {ex.StackTrace}");
            SetCorsHeaders(response);
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
        response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:5173");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}