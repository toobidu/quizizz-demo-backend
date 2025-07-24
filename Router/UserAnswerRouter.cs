using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

/// <summary>
/// Router xử lý các API liên quan đến User Answers
/// Quản lý việc submit và tracking câu trả lời của người chơi
/// </summary>
public class UserAnswerRouter : IBaseRouter
{
    // TODO: Cần tạo UserAnswerController khi implement
    // private readonly UserAnswerController _userAnswerController;

    public UserAnswerRouter()
    {
        // TODO: Inject UserAnswerController khi được tạo
        // _userAnswerController = userAnswerController;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        // Chỉ xử lý các request đến /api/user-answers
        if (!path.StartsWith("/api/user-answers"))
            return false;

        // Handle CORS preflight
        if (method.ToUpper() == "OPTIONS")
        {
            HttpResponseHelper.WriteOptionsResponse(response);
            return true;
        }

        try
        {
            switch ((method.ToUpper(), path))
            {
                // POST /api/user-answers - Submit câu trả lời
                case ("POST", "/api/user-answers"):
                    await HandleSubmitUserAnswer(request, response);
                    return true;

                // GET /api/user-answers/session/{sessionId} - Lấy answers của session
                case var (m, p) when m == "GET" && p.StartsWith("/api/user-answers/session/"):
                    await HandleGetUserAnswersBySession(request, response, path);
                    return true;

                // GET /api/user-answers/user/{userId}/session/{sessionId} - Lấy answers của user trong session
                case var (m, p) when m == "GET" && p.Contains("/user/") && p.Contains("/session/"):
                    await HandleGetUserAnswersByUserAndSession(request, response, path);
                    return true;

                // GET /api/user-answers/user/{userId} - Lấy tất cả answers của user
                case var (m, p) when m == "GET" && p.StartsWith("/api/user-answers/user/") && !p.Contains("/session/"):
                    await HandleGetUserAnswersByUser(request, response, path);
                    return true;

                // PUT /api/user-answers/{answerId}/score - Cập nhật điểm cho answer
                case var (m, p) when m == "PUT" && p.Contains("/score"):
                    await HandleUpdateAnswerScore(request, response, path);
                    return true;

                // DELETE /api/user-answers/{answerId} - Xóa answer
                case var (m, p) when m == "DELETE" && p.StartsWith("/api/user-answers/") && !p.Contains("/"):
                    await HandleDeleteUserAnswer(request, response, path);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "User Answers API endpoint không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi server khi xử lý User Answer API: " + ex.Message, path);
            return true;
        }
    }

    #region Handler Methods

    /// <summary>
    /// Submit câu trả lời của user
    /// POST /api/user-answers
    /// </summary>
    private async Task HandleSubmitUserAnswer(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream);
            var json = await reader.ReadToEndAsync();
            var submitRequest = JsonSerializer.Deserialize<SubmitUserAnswerRequest>(json, JsonSerializerConfig.DefaultOptions);

            if (submitRequest == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu request không hợp lệ", request.Url?.AbsolutePath);
                return;
            }

            // Validate required fields
            if (submitRequest.UserId <= 0 || submitRequest.SessionId <= 0 || 
                submitRequest.QuestionId <= 0 || submitRequest.AnswerId <= 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "UserId, SessionId, QuestionId và AnswerId là bắt buộc", request.Url?.AbsolutePath);
                return;
            }

            // TODO: Implement UserAnswerController.SubmitAnswerAsync(submitRequest)
            // var result = await _userAnswerController.SubmitAnswerAsync(submitRequest);
            
            // Temporary response
            var tempResponse = new
            {
                userAnswerId = new Random().Next(1, 1000),
                userId = submitRequest.UserId,
                sessionId = submitRequest.SessionId,
                questionId = submitRequest.QuestionId,
                answerId = submitRequest.AnswerId,
                timeToAnswer = submitRequest.TimeToAnswer,
                isCorrect = new Random().Next(0, 2) == 1, // Random để test
                score = new Random().Next(50, 100),
                submittedAt = DateTime.UtcNow,
                message = "User answer submitted successfully (TEMPORARY IMPLEMENTATION)"
            };

            HttpResponseHelper.WriteJsonResponse(response, 
                ApiResponse<object>.Success(tempResponse, "Submit câu trả lời thành công", 201));
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi submit câu trả lời: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Lấy tất cả answers của một session
    /// GET /api/user-answers/session/{sessionId}
    /// </summary>
    private async Task HandleGetUserAnswersBySession(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int sessionId))
            {
                HttpResponseHelper.WriteBadRequest(response, "Session ID không hợp lệ", path);
                return;
            }

            // TODO: Implement UserAnswerController.GetAnswersBySessionAsync(sessionId)
            // var result = await _userAnswerController.GetAnswersBySessionAsync(sessionId);

            HttpResponseHelper.WriteNotFound(response, "Get User Answers by Session API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy user answers theo session: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Lấy answers của user trong một session cụ thể
    /// GET /api/user-answers/user/{userId}/session/{sessionId}
    /// </summary>
    private async Task HandleGetUserAnswersByUserAndSession(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            // Parse path: /api/user-answers/user/{userId}/session/{sessionId}
            var pathParts = path.Split('/');
            if (pathParts.Length < 6 || 
                !int.TryParse(pathParts[3], out int userId) || 
                !int.TryParse(pathParts[5], out int sessionId))
            {
                HttpResponseHelper.WriteBadRequest(response, "User ID hoặc Session ID không hợp lệ", path);
                return;
            }

            // TODO: Implement UserAnswerController.GetAnswersByUserAndSessionAsync(userId, sessionId)
            // var result = await _userAnswerController.GetAnswersByUserAndSessionAsync(userId, sessionId);

            HttpResponseHelper.WriteNotFound(response, "Get User Answers by User and Session API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy user answers theo user và session: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Lấy tất cả answers của một user
    /// GET /api/user-answers/user/{userId}
    /// </summary>
    private async Task HandleGetUserAnswersByUser(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int userId))
            {
                HttpResponseHelper.WriteBadRequest(response, "User ID không hợp lệ", path);
                return;
            }

            // Parse query parameters for pagination
            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            int page = int.TryParse(queryParams["page"], out var p) ? p : 1;
            int limit = int.TryParse(queryParams["limit"], out var l) ? l : 50;

            // TODO: Implement UserAnswerController.GetAnswersByUserAsync(userId, page, limit)
            // var result = await _userAnswerController.GetAnswersByUserAsync(userId, page, limit);

            HttpResponseHelper.WriteNotFound(response, "Get User Answers by User API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy user answers theo user: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Cập nhật điểm cho một answer
    /// PUT /api/user-answers/{answerId}/score
    /// </summary>
    private async Task HandleUpdateAnswerScore(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int answerId))
            {
                HttpResponseHelper.WriteBadRequest(response, "Answer ID không hợp lệ", path);
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var json = await reader.ReadToEndAsync();
            var updateRequest = JsonSerializer.Deserialize<UpdateAnswerScoreRequest>(json, JsonSerializerConfig.DefaultOptions);

            if (updateRequest == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu update không hợp lệ", path);
                return;
            }

            // TODO: Implement UserAnswerController.UpdateAnswerScoreAsync(answerId, updateRequest)
            // var result = await _userAnswerController.UpdateAnswerScoreAsync(answerId, updateRequest);

            HttpResponseHelper.WriteNotFound(response, "Update Answer Score API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi cập nhật điểm answer: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Xóa một user answer
    /// DELETE /api/user-answers/{answerId}
    /// </summary>
    private async Task HandleDeleteUserAnswer(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int answerId))
            {
                HttpResponseHelper.WriteBadRequest(response, "Answer ID không hợp lệ", path);
                return;
            }

            // TODO: Implement UserAnswerController.DeleteAnswerAsync(answerId)
            // var result = await _userAnswerController.DeleteAnswerAsync(answerId);

            HttpResponseHelper.WriteNotFound(response, "Delete User Answer API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi xóa user answer: " + ex.Message, path);
        }
    }

    #endregion
}

#region Request Models

/// <summary>
/// Request model để submit user answer
/// </summary>
public class SubmitUserAnswerRequest
{
    public int UserId { get; set; }
    public int SessionId { get; set; }
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public double TimeToAnswer { get; set; }
}

/// <summary>
/// Request model để update answer score
/// </summary>
public class UpdateAnswerScoreRequest
{
    public int NewScore { get; set; }
    public string? Reason { get; set; }
}

#endregion
