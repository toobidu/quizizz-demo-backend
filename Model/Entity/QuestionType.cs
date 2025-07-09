namespace ConsoleApp1.Model.Entity;

public class QuestionType
{
    public int Id { get; set; }
    public string Name { get; set; }

    public QuestionType() { }

    public QuestionType(int id, string name)
    {
        Id = id;
        Name = name;
    }
}