using System.Net;
using System.Text.Json;
namespace ConsoleApp1.Config;
public static class HttpResponseHelper
{
    public static void WriteJsonResponse<T>(HttpListenerResponse response, ApiResponse<T> apiResponse)
    {
        SetCorsHeaders(response);
        response.StatusCode = apiResponse.Status;
        response.ContentType = "application/json";
        // Sử dụng JsonSerializerConfig để đảm bảo camelCase format
        string json = JsonSerializerConfig.SerializeCamelCase(apiResponse);
        using var writer = new StreamWriter(response.OutputStream);
        writer.Write(json);
    }
    public static void SetCorsHeaders(HttpListenerResponse response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
    }
    public static void WriteOptionsResponse(HttpListenerResponse response)
    {
        SetCorsHeaders(response);
        response.StatusCode = 200;
        response.ContentLength64 = 0;
    }
    public static void WriteSuccess<T>(HttpListenerResponse response, T data, string message, string? path = null)
    {
        var result = new ApiResponse<T>(
            status: (int)HttpStatusCode.OK,
            message: message,
            data: data,
            errorCode: null,
            path: path
        );
        WriteJsonResponse(response, result);
    }
    public static void WriteBadRequest(HttpListenerResponse response, string message, string? path = null)
    {
        var result = new ApiResponse<object>(
            status: (int)HttpStatusCode.BadRequest,
            message: message,
            data: null,
            errorCode: "BadRequest",
            path: path
        );
        WriteJsonResponse(response, result);
    }
    public static void WriteUnauthorized(HttpListenerResponse response, string message, string? path = null)
    {
        var result = new ApiResponse<object>(
            status: (int)HttpStatusCode.Unauthorized,
            message: message,
            data: null,
            errorCode: "Unauthorized",
            path: path
        );
        WriteJsonResponse(response, result);
    }
    public static void WriteForbidden(HttpListenerResponse response, string message, string? path = null)
    {
        var result = new ApiResponse<object>(
            status: (int)HttpStatusCode.Forbidden,
            message: message,
            data: null,
            errorCode: "Forbidden",
            path: path
        );
        WriteJsonResponse(response, result);
    }
    public static void WriteNotFound(HttpListenerResponse response, string message, string? path = null)
    {
        var result = new ApiResponse<object>(
            status: (int)HttpStatusCode.NotFound,
            message: message,
            data: null,
            errorCode: "NotFound",
            path: path
        );
        WriteJsonResponse(response, result);
    }
    public static void WriteInternalServerError(HttpListenerResponse response, string message, string? path = null)
    {
        var result = new ApiResponse<object>(
            status: (int)HttpStatusCode.InternalServerError,
            message: message,
            data: null,
            errorCode: "ServerError",
            path: path
        );
        WriteJsonResponse(response, result);
    }
}
