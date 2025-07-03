namespace ConsoleApp1.Model.Entity;

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    
    public ICollection<Answer> Answers { get; set; }
    public ICollection<Room> Rooms { get; set; }
}