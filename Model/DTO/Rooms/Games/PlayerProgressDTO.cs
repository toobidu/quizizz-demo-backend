namespace ConsoleApp1.Model.DTO.Rooms.Games;
public class PlayerProgressDTO
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public int CurrentScore { get; set; }
    public int CorrectAnswers { get; set; }
    public int TotalQuestions { get; set; }
    public double Accuracy { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public PlayerProgressDTO(int userId, string username, int currentScore, 
        int correctAnswers, int totalQuestions, double accuracy, TimeSpan averageResponseTime)
    {
        UserId = userId;
        Username = username;
        CurrentScore = currentScore;
        CorrectAnswers = correctAnswers;
        TotalQuestions = totalQuestions;
        Accuracy = accuracy;
        AverageResponseTime = averageResponseTime;
    }
}
