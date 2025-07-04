namespace ConsoleApp1.Model.Entity;

public class Answer
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string AnswerText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }

    public Answer(int id, int questionId, string answerText, bool isCorrect) =>
        (Id, QuestionId, AnswerText, IsCorrect) = (id, questionId, answerText, isCorrect);
}