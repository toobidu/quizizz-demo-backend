namespace ConsoleApp1.Model.Entity.Questions;

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public int? TopicId { get; set; }
    public int? QuestionTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Question() { }

    public Question(int id, string questionText, int? topicId, int? questionTypeId, 
                   DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        QuestionText = questionText;
        TopicId = topicId;
        QuestionTypeId = questionTypeId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}