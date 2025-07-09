namespace ConsoleApp1.Model.Entity;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; }

    public Topic() { }

    public Topic(int id, string name)
    {
        Id = id;
        Name = name;
    }
}