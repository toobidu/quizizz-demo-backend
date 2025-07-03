namespace ConsoleApp1.Model.Entity;

public class Question
{
    public int Id { get; set; }
    public string Text { get; set; }
    public string CorrectAnswer { get; set; } 

    public string OptionA { get; set; }
    public string OptionB { get; set; }
    public string OptionC { get; set; }
    public string OptionD { get; set; }
    
    public ICollection<Room> Rooms { get; set; }
}