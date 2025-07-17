using ConsoleApp1.Model.DTO.Game;

namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;

/// <summary>
/// Service xử lý câu trả lời và tính điểm
/// </summary>
public class AnswerProcessor
{
    /// <summary>
    /// Kiểm tra câu trả lời có đúng không
    /// </summary>
    public bool IsAnswerCorrect(QuestionData question, object selectedAnswer)
    {
        try
        {
            var selectedStr = selectedAnswer.ToString()?.Trim().ToLower();
            var correctStr = question.CorrectAnswer.Trim().ToLower();
            
            return selectedStr == correctStr;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANSWER] Lỗi kiểm tra tính đúng đắn của câu trả lời: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tính thời gian trả lời (giây)
    /// </summary>
    public int CalculateTimeToAnswer(DateTime gameStartTime, long submitTimestamp)
    {
        try
        {
            var submitTime = DateTimeOffset.FromUnixTimeMilliseconds(submitTimestamp).DateTime;
            var timeToAnswer = (submitTime - gameStartTime).TotalSeconds;
            return Math.Max(PlayerInteractionConstants.Scoring.MinTimeToAnswer, (int)timeToAnswer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ANSWER] Lỗi tính thời gian trả lời: {ex.Message}");
            return PlayerInteractionConstants.Scoring.DefaultTimeToAnswer;
        }
    }

    /// <summary>
    /// Tính điểm dựa trên độ chính xác và thời gian
    /// </summary>
    public int CalculatePoints(bool isCorrect, int timeToAnswer, QuestionData question)
    {
        if (!isCorrect) return 0;
        
        // Điểm cơ bản
        var basePoints = PlayerInteractionConstants.Scoring.BasePoints;
        
        // Bonus điểm dựa trên tốc độ (càng nhanh càng nhiều điểm)
        var maxTime = PlayerInteractionConstants.Scoring.MaxTimePerQuestion;
        var speedBonus = Math.Max(0, (maxTime - timeToAnswer) * PlayerInteractionConstants.Scoring.SpeedBonusMultiplier);
        
        var totalPoints = basePoints + speedBonus;
        
        Console.WriteLine($"[SCORING] Question answered in {timeToAnswer}s: {basePoints} base + {speedBonus} speed bonus = {totalPoints} points");
        
        return totalPoints;
    }

    /// <summary>
    /// Validate answer submission
    /// </summary>
    public (bool IsValid, string ErrorMessage) ValidateAnswerSubmission(
        PlayerAnswerSubmission submission, 
        PlayerGameSession gameSession,
        PlayerGameResult playerResult)
    {
        // Validate question index
        if (submission.QuestionIndex < 0 || submission.QuestionIndex >= gameSession.Questions.Count)
        {
            return (false, PlayerInteractionConstants.Messages.InvalidQuestionIndex);
        }

        // Check for duplicate answer
        var existingAnswer = playerResult.Answers.FirstOrDefault(a => a.QuestionIndex == submission.QuestionIndex);
        if (existingAnswer != null)
        {
            return (false, PlayerInteractionConstants.Messages.AlreadyAnswered);
        }

        // Validate answer length
        var answerStr = submission.SelectedAnswer.ToString();
        if (!string.IsNullOrEmpty(answerStr) && answerStr.Length > PlayerInteractionConstants.Limits.MaxAnswerLength)
        {
            return (false, "Câu trả lời quá dài");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Process answer và tạo PlayerAnswer object
    /// </summary>
    public PlayerAnswer ProcessAnswer(
        PlayerAnswerSubmission submission,
        QuestionData question,
        DateTime gameStartTime,
        string username)
    {
        var isCorrect = IsAnswerCorrect(question, submission.SelectedAnswer);
        var timeToAnswer = CalculateTimeToAnswer(gameStartTime, submission.SubmitTime);
        var pointsEarned = CalculatePoints(isCorrect, timeToAnswer, question);

        return new PlayerAnswer
        {
            Username = username,
            Answer = submission.SelectedAnswer,
            Timestamp = submission.SubmitTime,
            TimeToAnswer = timeToAnswer,
            IsCorrect = isCorrect,
            PointsEarned = pointsEarned,
            QuestionIndex = submission.QuestionIndex
        };
    }

    /// <summary>
    /// Tạo answer result event data
    /// </summary>
    public AnswerResultEventData CreateAnswerResultEventData(
        PlayerAnswer answer, 
        QuestionData question, 
        int totalScore)
    {
        return new AnswerResultEventData
        {
            QuestionIndex = answer.QuestionIndex,
            IsCorrect = answer.IsCorrect,
            CorrectAnswer = question.CorrectAnswer,
            PointsEarned = answer.PointsEarned,
            TotalScore = totalScore,
            TimeToAnswer = answer.TimeToAnswer
        };
    }
}