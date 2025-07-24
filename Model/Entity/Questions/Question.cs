using System.Collections.Generic;
namespace ConsoleApp1.Model.Entity.Questions;
public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int? TopicId { get; set; }
    public int? QuestionTypeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public QuestionType? QuestionType { get; set; }
    public List<Answer> Answers { get; set; } = new List<Answer>();
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
