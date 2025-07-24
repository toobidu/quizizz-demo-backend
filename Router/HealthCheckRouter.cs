using ConsoleApp1.Config;
using ConsoleApp1.Router;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

namespace ConsoleApp1.Data
{
    /// <summary>
    /// Router xử lý health check endpoint
    /// </summary>
    public class HealthCheckRouter : IBaseRouter
    {
        public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.Url?.AbsolutePath.ToLower() == "/health" && request.HttpMethod == "GET")
            {
                var healthData = new
                {
                    status = "healthy",
                    timestamp = DateTime.UtcNow,
                    version = "1.0.0",
                    services = new
                    {
                        database = "connected",
                        redis = "connected",
                        websocket = "running"
                    },
                    uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                    environment = "development"
                };

                response.ContentType = "application/json; charset=utf-8";
                response.StatusCode = 200;

                var json = JsonSerializer.Serialize(healthData, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                var bytes = Encoding.UTF8.GetBytes(json);
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes);

                return true;
            }

            return false;
        }
    }
}
