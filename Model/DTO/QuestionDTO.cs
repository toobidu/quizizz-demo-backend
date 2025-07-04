namespace ConsoleApp1.Model.DTO;

public class QuestionDTO
{
    public int Id { get; set; }
    public string QuestionText { get; set; }
    public List<AnswerDTO> Options { get; set; }
    
    public QuestionDTO(int id, string questionText, List<AnswerDTO> options) =>
        (Id, QuestionText, Options) = (id, questionText, options);
}