using ConsoleApp1.Model.DTO.Game;
using ConsoleApp1.Model.DTO.WebSocket;
using ConsoleApp1.Model.DTO.Questions;
namespace ConsoleApp1.Service.Helper;
/// <summary>
/// Helper tạo game events với format chuẩn hóa
/// Đảm bảo tất cả game-related WebSocket messages có camelCase format
/// </summary>
public static class GameEventHelper
{
    /// <summary>
    /// Tạo QUESTION_SENT event
    /// </summary>
    public static WebSocketMessage<GameQuestionEventData> CreateQuestionSentEvent(
        QuestionDTO question, 
        int questionIndex, 
        int totalQuestions, 
        int timeLimit = 30)
    {
        var questionData = new GameQuestionEventData
        {
            QuestionId = question.Id,
            QuestionIndex = questionIndex,
            QuestionText = question.QuestionText,
            Options = question.Options.Select(opt => new QuestionOptionData
            {
                Id = opt.Id,
                Text = opt.AnswerText,
                OptionIndex = opt.OptionIndex,
                IsCorrect = false // Don't reveal correct answer yet
            }).ToList(),
            QuestionType = GetQuestionTypeString(question.QuestionTypeId),
            TimeLimit = timeLimit,
            Points = question.Points,
            TotalQuestions = totalQuestions,
            CurrentQuestion = questionIndex + 1,
            Topic = string.Empty, // Will be filled from topic data
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddSeconds(timeLimit)
        };
        return new WebSocketMessage<GameQuestionEventData>("QUESTION_SENT", questionData);
    }
    /// <summary>
    /// Tạo ANSWER_RESULT event
    /// </summary>
    public static WebSocketMessage<AnswerResultData> CreateAnswerResultEvent(
        int questionId,
        int questionIndex,
        bool isCorrect,
        int correctOptionId,
        string correctAnswerText,
        int pointsEarned,
        int totalPoints,
        int timeToAnswer,
        int rank,
        int totalPlayers,
        string? explanation = null)
    {
        var resultData = new AnswerResultData
        {
            QuestionId = questionId,
            QuestionIndex = questionIndex,
            IsCorrect = isCorrect,
            CorrectOptionId = correctOptionId,
            CorrectOptionIndex = 0, // Will be determined from option data
            CorrectAnswerText = correctAnswerText,
            PointsEarned = pointsEarned,
            TotalPoints = totalPoints,
            TimeToAnswer = timeToAnswer,
            Rank = rank,
            TotalPlayers = totalPlayers,
            Explanation = explanation
        };
        return new WebSocketMessage<AnswerResultData>("ANSWER_RESULT", resultData);
    }
    /// <summary>
    /// Tạo GAME_PROGRESS event
    /// </summary>
    public static WebSocketMessage<GameProgressData> CreateGameProgressEvent(
        int currentQuestionIndex,
        int totalQuestions,
        int timeRemaining,
        string gameState,
        int playersAnswered,
        int totalPlayers)
    {
        var progressData = new GameProgressData
        {
            CurrentQuestionIndex = currentQuestionIndex,
            TotalQuestions = totalQuestions,
            GameProgress = totalQuestions > 0 ? (double)currentQuestionIndex / totalQuestions * 100 : 0,
            TimeRemaining = timeRemaining,
            GameState = gameState,
            PlayersAnswered = playersAnswered,
            TotalPlayers = totalPlayers
        };
        return new WebSocketMessage<GameProgressData>("GAME_PROGRESS", progressData);
    }
    /// <summary>
    /// Tạo NEXT_QUESTION event
    /// </summary>
    public static WebSocketMessage<object> CreateNextQuestionEvent(string roomCode, int nextQuestionIndex)
    {
        var data = new
        {
            roomCode = roomCode,
            nextQuestionIndex = nextQuestionIndex,
            message = "Đang chuyển sang câu hỏi tiếp theo..."
        };
        return new WebSocketMessage<object>("NEXT_QUESTION", data);
    }
    /// <summary>
    /// Tạo GAME_STARTED event
    /// </summary>
    public static WebSocketMessage<object> CreateGameStartedEvent(
        string roomCode,
        int totalQuestions,
        int timeLimit,
        List<string> playerNames)
    {
        var data = new
        {
            roomCode = roomCode,
            totalQuestions = totalQuestions,
            timeLimit = timeLimit,
            players = playerNames,
            message = "Game đã bắt đầu! Chuẩn bị cho câu hỏi đầu tiên...",
            startTime = DateTime.UtcNow
        };
        return new WebSocketMessage<object>("GAME_STARTED", data);
    }
    /// <summary>
    /// Tạo GAME_FINISHED event với leaderboard
    /// </summary>
    public static WebSocketMessage<object> CreateGameFinishedEvent(
        string roomCode,
        List<object> leaderboard,
        TimeSpan gameDuration)
    {
        var data = new
        {
            roomCode = roomCode,
            leaderboard = leaderboard,
            gameDuration = gameDuration.TotalSeconds,
            message = "Game đã kết thúc! Xem kết quả cuối cùng...",
            finishTime = DateTime.UtcNow
        };
        return new WebSocketMessage<object>("GAME_FINISHED", data);
    }
    /// <summary>
    /// Parse PlayerAnswerSubmission từ WebSocket message
    /// </summary>
    public static PlayerAnswerSubmission? ParseAnswerSubmission(object answerData)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(answerData);
            return System.Text.Json.JsonSerializer.Deserialize<PlayerAnswerSubmission>(json, 
                new System.Text.Json.JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                });
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    /// <summary>
    /// Validate PlayerAnswerSubmission
    /// </summary>
    public static (bool IsValid, string ErrorMessage) ValidateAnswerSubmission(PlayerAnswerSubmission submission)
    {
        if (submission.QuestionId <= 0)
            return (false, "Invalid question ID");
        if (submission.SelectedOptionId <= 0 && string.IsNullOrWhiteSpace(submission.TextAnswer))
            return (false, "No answer provided");
        if (submission.TimeToAnswer < 0)
            return (false, "Invalid time to answer");
        if (string.IsNullOrWhiteSpace(submission.RoomCode))
            return (false, "Room code is required");
        return (true, string.Empty);
    }
    /// <summary>
    /// Convert QuestionTypeId to string
    /// </summary>
    private static string GetQuestionTypeString(int questionTypeId)
    {
        return questionTypeId switch
        {
            1 => "multiple_choice",
            2 => "true_false",
            3 => "text_input",
            _ => "multiple_choice"
        };
    }
}
