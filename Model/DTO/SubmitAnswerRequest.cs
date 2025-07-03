namespace ConsoleApp1.Model.DTO;

public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public bool ValidField() => AnswerId > 0 && QuestionId > 0;
}