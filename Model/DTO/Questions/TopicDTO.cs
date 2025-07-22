namespace ConsoleApp1.Model.DTO.Questions;
public class TopicDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int QuestionCount { get; set; }
    public TopicDTO(int id, string name, int questionCount) =>
        (Id, Name, QuestionCount) = (id, name, questionCount);
}
