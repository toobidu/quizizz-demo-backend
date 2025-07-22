namespace ConsoleApp1.Model.DTO.Questions;
public class QuestionStatisticsDTO
{
    public int QuestionId { get; set; }
    public int TotalResponses { get; set; }
    public int CorrectResponses { get; set; }
    public Dictionary<int, int> AnswerDistribution { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public QuestionStatisticsDTO(int questionId, int totalResponses, int correctResponses, 
        Dictionary<int, int> answerDistribution, TimeSpan averageResponseTime)
    {
        QuestionId = questionId;
        TotalResponses = totalResponses;
        CorrectResponses = correctResponses;
        AnswerDistribution = answerDistribution;
        AverageResponseTime = averageResponseTime;
    }
}
