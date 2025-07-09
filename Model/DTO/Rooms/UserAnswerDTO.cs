namespace ConsoleApp1.Model.DTO;

public class UserAnswerDTO
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
    public int SelectedAnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public TimeSpan TimeTaken { get; set; }
    
    public UserAnswerDTO(int userId, int questionId, int selectedAnswerId, bool isCorrect, TimeSpan timeTaken) =>
        (UserId, QuestionId, SelectedAnswerId, IsCorrect, TimeTaken) = (userId, questionId, selectedAnswerId, isCorrect, timeTaken);
}