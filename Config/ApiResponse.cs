public class ApiResponse<T>
{
    public int Status { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? Path { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiResponse(int status, string message, T? data, string? errorCode, string? path)
    {
        Status = status;
        Message = message;
        Data = data;
        ErrorCode = errorCode;
        Path = path;
    }

    public static ApiResponse<T> Success(T data, string message = "Success", int status = 200, string? path = null)
        => new(status, message, data, null, path);

    public static ApiResponse<T> Fail(string message, int status = 400, string? errorCode = "UNKNOWN_ERROR",
        string? path = null)
        => new(status, message, default, errorCode, path);
}