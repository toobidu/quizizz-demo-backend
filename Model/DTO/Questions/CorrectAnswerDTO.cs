namespace ConsoleApp1.Model.DTO.Questions;

public class CorrectAnswerDTO
{
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    
    public bool IsCorrect { get; set; }
    
    public CorrectAnswerDTO(int questionId, int answerId, string answerText, bool isCorrect) =>
        (QuestionId, AnswerId, AnswerText, IsCorrect) = (questionId, answerId, answerText, isCorrect);
}