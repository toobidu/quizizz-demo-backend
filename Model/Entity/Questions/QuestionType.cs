namespace ConsoleApp1.Model.Entity.Questions;
public class QuestionType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public QuestionType() { }
    public QuestionType(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
