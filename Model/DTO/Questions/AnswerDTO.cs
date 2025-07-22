namespace ConsoleApp1.Model.DTO.Questions;
public class AnswerDTO
{
    public int Id { get; set; }
    public string AnswerText { get; set; }
    public int OptionIndex { get; set; }
    public AnswerDTO(int id, string answerText, int optionIndex) =>
        (Id, AnswerText, OptionIndex) = (id, answerText, optionIndex);
}
