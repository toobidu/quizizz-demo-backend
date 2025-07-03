namespace ConsoleApp1.Model.DTO;

public class QuestionDTO
{
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public List<AnswerDTO> Options { get; set; }
}