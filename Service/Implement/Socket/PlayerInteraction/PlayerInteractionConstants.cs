namespace ConsoleApp1.Service.Implement.Socket.PlayerInteraction;

/// <summary>
/// Constants cho Player Interaction Service
/// </summary>
public static class PlayerInteractionConstants
{
    /// <summary>
    /// WebSocket event names
    /// </summary>
    public static class Events
    {
        public const string AnswerResult = "answer-result";
        public const string PlayerFinished = "player-finished";
        public const string ScoreboardUpdate = "scoreboard-update";
        public const string PlayerStatusChanged = "player-status-changed";
        public const string QuestionCompleted = "question-completed";
        public const string GameCompleted = "game-completed";
        public const string Error = "error";
    }

    /// <summary>
    /// Player statuses
    /// </summary>
    public static class PlayerStatuses
    {
        public const string Waiting = "waiting";
        public const string Answering = "answering";
        public const string Answered = "answered";
        public const string Finished = "finished";
        public const string Online = "online";
        public const string Offline = "offline";
    }

    /// <summary>
    /// Game completion reasons
    /// </summary>
    public static class CompletionReasons
    {
        public const string AllFinished = "all-finished";
        public const string Timeout = "timeout";
        public const string HostEnded = "host-ended";
    }

    /// <summary>
    /// Scoring constants
    /// </summary>
    public static class Scoring
    {
        public const int BasePoints = 100;
        public const int SpeedBonusMultiplier = 2;
        public const int MaxTimePerQuestion = 30;
        public const int MinTimeToAnswer = 1;
        public const int DefaultTimeToAnswer = 30;
    }

    /// <summary>
    /// Messages
    /// </summary>
    public static class Messages
    {
        public const string NoActiveSession = "Không có game nào đang diễn ra";
        public const string GameEnded = "Game đã kết thúc";
        public const string InvalidAnswerFormat = "Định dạng câu trả lời không hợp lệ";
        public const string InvalidQuestionIndex = "Câu hỏi không hợp lệ";
        public const string AlreadyAnswered = "Bạn đã trả lời câu hỏi này rồi";
        public const string PlayerFinished = "Bạn đã hoàn thành tất cả câu hỏi!";
        public const string AllPlayersAnswered = "Tất cả người chơi đã trả lời câu hỏi này";
        public const string AllPlayersFinished = "Tất cả người chơi đã hoàn thành!";
        public const string AnswerProcessingError = "Lỗi xử lý câu trả lời";
    }

    /// <summary>
    /// Validation limits
    /// </summary>
    public static class Limits
    {
        public const int MaxAnswerLength = 1000;
        public const int MaxPlayersPerGame = 50;
        public const int MaxQuestionsPerGame = 100;
    }
}