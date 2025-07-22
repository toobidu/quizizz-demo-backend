namespace ConsoleApp1.Service.Implement.Socket.Scoring;
/// <summary>
/// Constants và cấu hình cho Scoring Service
/// </summary>
public static class ScoringConstants
{
    /// <summary>
    /// WebSocket event names
    /// </summary>
    public static class Events
    {
        public const string ScoreboardUpdated = "scoreboard-updated";
        public const string FinalResults = "final-results";
        public const string GameEnded = "game-ended";
        public const string Scoreboard = "scoreboard";
        public const string PersonalScoreboard = "personal-scoreboard";
        public const string PersonalFinalResult = "personal-final-result";
    }
    /// <summary>
    /// Achievement types
    /// </summary>
    public static class Achievements
    {
        public const string PerfectScore = "Perfect Score";
        public const string SpeedDemon = "Speed Demon";
        public const string StreakMaster = "Streak Master";
    }
    /// <summary>
    /// Achievement icons
    /// </summary>
    public static class Icons
    {
        public const string Trophy = "🏆";
        public const string Lightning = "⚡";
        public const string Fire = "🔥";
    }
    /// <summary>
    /// Scoring thresholds
    /// </summary>
    public static class Thresholds
    {
        public const int MinStreakForAchievement = 5;
        public const int SessionCleanupDelayMinutes = 5;
        public const int GameStartTimeEstimateMinutes = 10;
    }
    /// <summary>
    /// Game states
    /// </summary>
    public static class GameStates
    {
        public const string Finished = "finished";
        public const string Active = "active";
        public const string Waiting = "waiting";
    }
    /// <summary>
    /// Scoreboard types
    /// </summary>
    public static class ScoreboardTypes
    {
        public const string Current = "current";
        public const string Final = "final";
        public const string Personal = "personal";
    }
    /// <summary>
    /// Position change types
    /// </summary>
    public static class PositionChanges
    {
        public const string Up = "up";
        public const string Down = "down";
        public const string Same = "same";
    }
}
