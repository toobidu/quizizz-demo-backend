using System.Net;
using System.Text;
using System.Text.Json;
using ConsoleApp1.Controller;
using ConsoleApp1.Model.DTO;

public class HttpServer
{
    private readonly HttpListener _listener = new();
    private readonly AuthController _authController;

    public HttpServer(string urlPrefix, AuthController authController)
    {
        _listener.Prefixes.Add(urlPrefix);
        _authController = authController;
    }

    public async Task StartAsync()
    {
        _listener.Start();
        Console.WriteLine("🟢 HTTP server started on: " + _listener.Prefixes.First());

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

        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        try
        {
            if (method == "POST" && path == "/api/auth/register")
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"Received request body: {body}");
                
                var dto = JsonSerializer.Deserialize<RegisterRequest>(body);
                Console.WriteLine($"Deserialized DTO: {JsonSerializer.Serialize(dto)}");

                string result = await _authController.RegisterApi(dto!);
                await WriteJson(response, new { message = result });
            }
            else if (method == "POST" && path == "/api/auth/login")
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"Login request body: {body}"); 

                var dto = JsonSerializer.Deserialize<LoginRequest>(body);
                Console.WriteLine($"Deserialized login DTO: {JsonSerializer.Serialize(dto)}"); // Thêm log


                var result = await _authController.LoginApi(dto!);
                if (result == null)
                {
                    response.StatusCode = 401;
                    await WriteJson(response, new { error = "Đăng nhập thất bại" });
                }
                else
                {
                    await WriteJson(response, result);
                }
            }
            else if (method == "POST" && path == "/api/auth/logout")
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                var jsonDoc = JsonDocument.Parse(body);
                var token = jsonDoc.RootElement.GetProperty("token").GetString();

                string result = await _authController.LogoutApi(token!);
                await WriteJson(response, new { message = result });
            }
            // else if (method == "GET" && path == "/api/auth/permission-check")
            // {
            //     string? permission = request.QueryString["permission"];
            //     string? authHeader = request.Headers["Authorization"];
            //     string? token = authHeader?.Replace("Bearer ", "");
            //
            //     if (string.IsNullOrWhiteSpace(permission) || string.IsNullOrWhiteSpace(token))
            //     {
            //         response.StatusCode = 400;
            //         await WriteJson(response, new { error = "Thiếu token hoặc permission" });
            //     }
            //     else
            //     {
            //         var result = await _authController.CheckPermissionApi(token, permission);
            //         await WriteJson(response, new { message = result });
            //     }
            // }
            else
            {
                response.StatusCode = 404;
                await WriteJson(response, new { error = "404 Not Found" });
            }
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await WriteJson(response, new { error = "🔥 Internal Server Error", detail = ex.Message });
        }

        response.OutputStream.Close();
    }

    private async Task WriteJson(HttpListenerResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentType = "application/json";
        response.ContentEncoding = Encoding.UTF8;
        response.ContentLength64 = buffer.Length;
        await response.OutputStream.WriteAsync(buffer);
    }
}
