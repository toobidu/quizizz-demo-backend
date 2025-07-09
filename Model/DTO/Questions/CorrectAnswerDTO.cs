namespace ConsoleApp1.Model.DTO;

public class CorrectAnswerDTO
{
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public string AnswerText { get; set; }
    
    public bool isCorrect { get; set; }
    
    public CorrectAnswerDTO(int questionId, int answerId, string answerText, bool isCorrect) =>
        (QuestionId, AnswerId, AnswerText, isCorrect) = (questionId, answerId, answerText, isCorrect);
}