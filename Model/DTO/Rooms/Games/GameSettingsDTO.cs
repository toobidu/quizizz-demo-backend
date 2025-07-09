namespace ConsoleApp1.Model.DTO.Rooms.Games;

public class GameSettingsDTO
{
    public int TimePerQuestion { get; set; }
    public bool ShowAnswersAfterQuestion { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public int PointsPerQuestion { get; set; }
    public bool EnableTimer { get; set; }
    
    public GameSettingsDTO(int timePerQuestion, bool showAnswersAfterQuestion, 
        bool shuffleQuestions, bool shuffleAnswers, int pointsPerQuestion, bool enableTimer)
    {
        TimePerQuestion = timePerQuestion;
        ShowAnswersAfterQuestion = showAnswersAfterQuestion;
        ShuffleQuestions = shuffleQuestions;
        ShuffleAnswers = shuffleAnswers;
        PointsPerQuestion = pointsPerQuestion;
        EnableTimer = enableTimer;
    }
}