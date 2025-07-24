namespace ConsoleApp1.Model.DTO.Rooms.Games;
using ConsoleApp1.Model.DTO.Questions;
public class GameQuestionDTO
{
    public int GameSessionId { get; set; }
    public int QuestionId { get; set; }
    public int QuestionOrder { get; set; }
    public int TimeLimit { get; set; }
    public required QuestionDTO Question { get; set; }
}
