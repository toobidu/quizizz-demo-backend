using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO.Questions;

/// <summary>
/// DTO cho câu hỏi kèm theo danh sách câu trả lời theo chủ đề
/// Được sử dụng khi lấy dữ liệu từ JOIN query của topics, questions, answers
/// </summary>
public class QuestionWithAnswersDTO
{
    [JsonPropertyName("topicName")]
    public string TopicName { get; set; } = string.Empty;
    
    [JsonPropertyName("questionId")]
    public int QuestionId { get; set; }
    
    [JsonPropertyName("questionText")]
    public string QuestionText { get; set; } = string.Empty;
    
    [JsonPropertyName("answers")]
    public List<AnswerDetailDTO> Answers { get; set; } = new();
}

/// <summary>
/// DTO chi tiết cho từng câu trả lời
/// </summary>
public class AnswerDetailDTO
{
    [JsonPropertyName("answerId")]
    public int AnswerId { get; set; }
    
    [JsonPropertyName("answerText")]
    public string AnswerText { get; set; } = string.Empty;
    
    [JsonPropertyName("isCorrect")]
    public bool IsCorrect { get; set; }
}

/// <summary>
/// DTO raw từ database cho JOIN query
/// Sử dụng để mapping từ kết quả SQL JOIN
/// </summary>
public class QuestionAnswerRawDTO
{
    public string TopicName { get; set; } = string.Empty;
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int AnswerId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}
