namespace ConsoleApp1.Model.DTO;

public class QuestionDTO
{
    public int Id { get; set; }
    public string Text { get; set; }
    public string OptionA { get; set; }
    public string OptionB { get; set; }
    public string OptionC { get; set; }
    public string OptionD { get; set; }

    public QuestionDTO(int id, string text, string optionA, string optionB, string optionC, string optionD)
    {
        Id = id;
        Text = text;
        OptionA = optionA;
        OptionB = optionB;
        OptionC = optionC;
        OptionD = optionD;
    }
}