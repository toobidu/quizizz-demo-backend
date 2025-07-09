namespace ConsoleApp1.Model.Entity.Questions;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; }
    public bool IsCorrect { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Answer() { }

    public Answer(int id, int questionId, string answerText, bool isCorrect, 
                 DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        QuestionId = questionId;
        AnswerText = answerText;
        IsCorrect = isCorrect;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}