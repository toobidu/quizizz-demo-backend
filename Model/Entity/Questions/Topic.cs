namespace ConsoleApp1.Model.Entity.Questions;
public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Topic() { }
    public Topic(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
