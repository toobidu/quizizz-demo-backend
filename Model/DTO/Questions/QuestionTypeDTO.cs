namespace ConsoleApp1.Model.DTO.Questions;

public class QuestionTypeDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    public QuestionTypeDTO(int id, string name) =>
        (Id, Name) = (id, name);
}