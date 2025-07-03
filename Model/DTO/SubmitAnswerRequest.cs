namespace ConsoleApp1.Model.DTO;

public class SubmitAnswerRequest
{
    public int QuestionId { get; set; }
    public string SelectedAnswer { get; set; }

    public SubmitAnswerRequest(int questionId, string selectedAnswer)
    {
        QuestionId = questionId;
        SelectedAnswer = selectedAnswer;
    }

    public bool ValidField()
    {
        return SelectedAnswer is "A" or "B" or "C" or "D";
    }
}