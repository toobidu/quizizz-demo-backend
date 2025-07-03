namespace ConsoleApp1.Model.DTO;

public class UserAnswerDTO
{
    public int UserId { get; set; }
    public int QuestionId { get; set; }
    public int SelectedAnswerId { get; set; }
    public bool IsCorrect { get; set; }
    public string TimeTaken { get; set; }
}