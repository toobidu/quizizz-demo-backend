namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;

/// <summary>
/// Models cho Room Management Service
/// </summary>
public class RoomPlayerInfo
{
    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; }
    public bool IsHost { get; set; }
    public DateTime? JoinTime { get; set; }
    public bool IsOnline { get; set; }
}

/// <summary>
/// Room update event data
/// </summary>
public class RoomUpdateEventData
{
    public string RoomCode { get; set; } = string.Empty;
    public List<RoomPlayerInfo> Players { get; set; } = new();
    public int TotalPlayers { get; set; }
    public string? Host { get; set; }
}

/// <summary>
/// Host change event data
/// </summary>
public class HostChangeEventData
{
    public string NewHost { get; set; } = string.Empty;
    public int NewHostId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Room join event data
/// </summary>
public class RoomJoinEventData
{
    public string RoomCode { get; set; } = string.Empty;
    public bool IsHost { get; set; }
    public string Message { get; set; } = string.Empty;
}