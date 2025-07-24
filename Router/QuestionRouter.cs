using System.Net;
using System.Text;
using System.Web;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

/// <summary>
/// Router xử lý các API liên quan đến câu hỏi (Questions)
/// Bao gồm: lấy câu hỏi theo chủ đề, lấy câu hỏi với câu trả lời, etc.
/// </summary>
public class QuestionRouter : IBaseRouter
{
    private readonly QuestionController _controller;

    public QuestionRouter(QuestionController controller)
    {
        _controller = controller;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;
        
        // Chỉ xử lý các API bắt đầu với /api/questions
        if (!path.StartsWith("/api/questions")) return false;

        try
        {
            switch ((method.ToUpper(), path))
            {
                // GET /api/questions/by-topic-name?topicName=Toán học
                case ("GET", "/api/questions/by-topic-name"):
                    await HandleGetQuestionsByTopicName(request, response);
                    return true;

                // GET /api/questions/with-answers/by-topic-name?topicName=Toán học  
                case ("GET", "/api/questions/with-answers/by-topic-name"):
                    await HandleGetQuestionsWithAnswersByTopicName(request, response);
                    return true;

                // GET /api/questions/by-topic/{topicId}
                case var (m, p) when m == "GET" && p.StartsWith("/api/questions/by-topic/"):
                    await HandleGetQuestionsByTopicId(request, response, path);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "API endpoint không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi server khi xử lý API questions: " + ex.Message, path);
            return true;
        }
    }

    /// <summary>
    /// Xử lý API lấy câu hỏi kèm theo câu trả lời theo tên chủ đề
    /// GET /api/questions/with-answers/by-topic-name?topicName=Toán học
    /// </summary>
    private async Task HandleGetQuestionsWithAnswersByTopicName(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // Lấy parameter topicName từ query string
            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            string? topicName = queryParams["topicName"];

            if (string.IsNullOrWhiteSpace(topicName))
            {
                HttpResponseHelper.WriteBadRequest(response, 
                    "Parameter 'topicName' là bắt buộc. Ví dụ: ?topicName=Toán học", 
                    request.Url?.AbsolutePath);
                return;
            }

            // Gọi controller để lấy dữ liệu
            var result = await _controller.GetQuestionsWithAnswersByTopicNameAsync(topicName);
            
            // Trả về response dạng JSON với format chuẩn
            HttpResponseHelper.WriteJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy câu hỏi với câu trả lời: " + ex.Message, 
                request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Xử lý API lấy câu hỏi theo tên chủ đề (không có câu trả lời)
    /// GET /api/questions/by-topic-name?topicName=Toán học
    /// </summary>
    private async Task HandleGetQuestionsByTopicName(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            string? topicName = queryParams["topicName"];

            if (string.IsNullOrWhiteSpace(topicName))
            {
                HttpResponseHelper.WriteBadRequest(response, 
                    "Parameter 'topicName' là bắt buộc", 
                    request.Url?.AbsolutePath);
                return;
            }

            // Gọi controller để lấy dữ liệu
            var result = await _controller.GetQuestionsByTopicNameAsync(topicName);
            
            // Trả về response dạng JSON với format chuẩn
            HttpResponseHelper.WriteJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy câu hỏi: " + ex.Message, 
                request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Xử lý API lấy câu hỏi theo ID chủ đề
    /// GET /api/questions/by-topic/{topicId}
    /// </summary>
    private async Task HandleGetQuestionsByTopicId(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            // Parse topicId từ URL path
            var pathParts = path.Split('/');
            if (pathParts.Length < 5 || !int.TryParse(pathParts[4], out int topicId))
            {
                HttpResponseHelper.WriteBadRequest(response, 
                    "Topic ID không hợp lệ. Ví dụ: /api/questions/by-topic/1", 
                    path);
                return;
            }

            // Gọi controller method đã có sẵn
            var result = await _controller.GetQuestionsByTopicAsync(topicId);
            HttpResponseHelper.WriteJsonResponse(response, result);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy câu hỏi theo topic ID: " + ex.Message, 
                path);
        }
    }
}
