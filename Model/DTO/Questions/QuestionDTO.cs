namespace ConsoleApp1.Model.DTO.Questions;
public class QuestionDTO
{
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public List<AnswerDTO> Options { get; set; }
    public int TopicId { get; set; }
    public int QuestionTypeId { get; set; }
    public int TimeLimit { get; set; }
    public int Points { get; set; }
    public QuestionDTO(int id, string questionText, List<AnswerDTO> options, 
        int topicId, int questionTypeId, int timeLimit, int points)
    {
        Id = id;
        QuestionText = questionText;
        Options = options;
        TopicId = topicId;
        QuestionTypeId = questionTypeId;
        TimeLimit = timeLimit;
        Points = points;
    }
}
