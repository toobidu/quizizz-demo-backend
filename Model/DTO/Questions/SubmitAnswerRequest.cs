namespace ConsoleApp1.Model.DTO.Questions;
public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public int AnswerId { get; set; }
    public bool ValidField() => AnswerId > 0 && QuestionId > 0;
    public SubmitAnswerRequest(int questionId, int answerId) =>
        (QuestionId, AnswerId) = (questionId, answerId);
}
