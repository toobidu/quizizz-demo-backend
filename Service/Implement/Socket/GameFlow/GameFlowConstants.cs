namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Constants cho Game Flow Service
/// </summary>
public static class GameFlowConstants
{
    /// <summary>
    /// WebSocket event names
    /// </summary>
    public static class Events
    {
        public const string GameStarted = "game-started";
        public const string NewQuestion = "new-question";
        public const string NextQuestion = "next-question";
        public const string TimerUpdate = "timer-update";
        public const string Countdown = "countdown";
        public const string ProgressUpdate = "progress-update";
        public const string PlayerProgress = "player-progress";
        public const string PlayerFinished = "player-finished";
        public const string GameEnded = "game-ended";
        public const string GameStateChanged = "game-state-changed";
    }

    /// <summary>
    /// Game states
    /// </summary>
    public static class GameStates
    {
        public const string Waiting = "waiting";
        public const string Countdown = "countdown";
        public const string Playing = "playing";
        public const string Ended = "ended";
        public const string QuestionActive = "question-active";
    }

    /// <summary>
    /// Game end reasons
    /// </summary>
    public static class EndReasons
    {
        public const string Timeout = "timeout";
        public const string AllFinished = "all-finished";
        public const string HostEnded = "host-ended";
        public const string Error = "error";
    }

    /// <summary>
    /// Default values
    /// </summary>
    public static class Defaults
    {
        public const int GameTimeLimit = 300; // 5 minutes
        public const int CountdownSeconds = 3;
        public const int TimerUpdateInterval = 1; // 1 second
        public const int CleanupDelaySeconds = 10;
    }

    /// <summary>
    /// Thông báo hệ thống
    /// </summary>
    public static class Messages
    {
        public const string GameStarted = "Game đã bắt đầu!";
        public const string GameEndedTimeout = "Game đã kết thúc do hết thời gian!";
        public const string GameEndedAllFinished = "Tất cả người chơi đã hoàn thành!";
        public const string PlayerFinished = "Bạn đã hoàn thành tất cả câu hỏi!";
        public const string CountdownStart = "Bắt đầu!";
        public const string NoActiveSession = "Không có game nào đang diễn ra";
        public const string NoQuestions = "Không có câu hỏi nào được cung cấp";
        public const string PlayerNotFound = "Người chơi không tồn tại trong game session";
        public const string AllQuestionsCompleted = "Bạn đã hoàn thành tất cả câu hỏi!";
        public const string RoomNotFound = "Không tìm thấy phòng";
        public const string NoPlayersInRoom = "Phòng không có người chơi nào";
        public const string InvalidTimeLimit = "Thời gian giới hạn không hợp lệ";
        public const string GamePaused = "Game đã được tạm dừng";
        public const string GameResumed = "Game đã được tiếp tục";
    }

    /// <summary>
    /// Limits
    /// </summary>
    public static class Limits
    {
        public const int MaxGameTimeLimit = 3600; // 1 hour
        public const int MinGameTimeLimit = 30; // 30 seconds
        public const int MaxQuestionsPerGame = 100;
        public const int MaxPlayersPerGame = 50;
    }
}