namespace ConsoleApp1.Model.Entity;

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;

    public Question(int id, string questionText) => (Id, QuestionText) = (id, questionText);
}