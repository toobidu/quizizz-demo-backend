namespace ConsoleApp1.Model.DTO.Rooms;

/// <summary>
/// Request model để update user rank
/// </summary>
public class UpdateUserRankRequest
{
    public int UserId { get; set; }
    public int ScoreToAdd { get; set; }
    public int GamesPlayedToAdd { get; set; } = 1;
    public bool GameWon { get; set; } = false;
    public string? GameType { get; set; }
}

/// <summary>
/// Request model để khởi tạo rank cho user mới
/// </summary>
public class InitializeUserRankRequest
{
    public int UserId { get; set; }
    public string InitialLevel { get; set; } = "Bronze";
    public int InitialScore { get; set; } = 0;
}

/// <summary>
/// Request model để update user level
/// </summary>
public class UpdateUserLevelRequest
{
    public string NewLevel { get; set; } = "";
    public int ExperiencePoints { get; set; }
    public string? Reason { get; set; }
}