using ConsoleApp1.Model.DTO.Rooms.Games;

namespace ConsoleApp1.Mapper.Rooms;

public static class GameSettingsMapper
{
    public static Dictionary<string, string> ToEntity(GameSettingsDTO settings)
    {
        return new Dictionary<string, string>
        {
            { "TimePerQuestion", settings.TimePerQuestion.ToString() },
            { "ShowAnswersAfterQuestion", settings.ShowAnswersAfterQuestion.ToString() },
            { "ShuffleQuestions", settings.ShuffleQuestions.ToString() },
            { "ShuffleAnswers", settings.ShuffleAnswers.ToString() },
            { "PointsPerQuestion", settings.PointsPerQuestion.ToString() },
            { "EnableTimer", settings.EnableTimer.ToString() }
        };
    }

    public static GameSettingsDTO ToDTO(Dictionary<string, string> settings)
    {
        return new GameSettingsDTO(
            timePerQuestion: int.Parse(settings.GetValueOrDefault("TimePerQuestion", "30")),
            showAnswersAfterQuestion: bool.Parse(settings.GetValueOrDefault("ShowAnswersAfterQuestion", "true")),
            shuffleQuestions: bool.Parse(settings.GetValueOrDefault("ShuffleQuestions", "true")),
            shuffleAnswers: bool.Parse(settings.GetValueOrDefault("ShuffleAnswers", "true")),
            pointsPerQuestion: int.Parse(settings.GetValueOrDefault("PointsPerQuestion", "100")),
            enableTimer: bool.Parse(settings.GetValueOrDefault("EnableTimer", "true"))
        );
    }
}
