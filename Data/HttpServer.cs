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

        try
        {
            foreach (var router in _routers)
            {
                bool handled = await router.HandleAsync(request, response);
                if (handled)
                {
                    response.OutputStream.Close();
                    return;
                }
            }

            response.StatusCode = 404;
            await WriteJson(response, new { error = "404 Not Found" });
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
}