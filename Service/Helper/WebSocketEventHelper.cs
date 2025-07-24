using ConsoleApp1.Model.DTO.WebSocket;
using ConsoleApp1.Model.DTO.Game;
using ConsoleApp1.Service.Implement.Socket.RoomManagement;
using ConsoleApp1.Config;
namespace ConsoleApp1.Service.Helper;
/// <summary>
/// Helper class để tạo WebSocket events với format chuẩn hóa
/// Đảm bảo tất cả WebSocket messages đều có format kebab-case nhất quán
/// </summary>
public static class WebSocketEventHelper
{
    /// <summary>
    /// Tạo player-joined event
    /// </summary>
    public static WebSocketMessage<PlayerJoinedData> CreatePlayerJoinedEvent(int userId, string username, string roomCode, bool isHost)
    {
        var data = new PlayerJoinedData
        {
            UserId = userId,
            Username = username,
            RoomCode = roomCode,
            IsHost = isHost,
            JoinTime = DateTime.UtcNow
        };
        return new WebSocketMessage<PlayerJoinedData>(RoomManagementConstants.Events.PlayerJoined, data);
    }
    /// <summary>
    /// Tạo player-left event
    /// </summary>
    public static WebSocketMessage<PlayerLeftData> CreatePlayerLeftEvent(int userId, string username, string roomCode)
    {
        var data = new PlayerLeftData
        {
            UserId = userId,
            Username = username,
            RoomCode = roomCode
        };
        return new WebSocketMessage<PlayerLeftData>(RoomManagementConstants.Events.PlayerLeft, data);
    }
    /// <summary>
    /// Tạo room-players-updated event
    /// </summary>
    public static WebSocketMessage<RoomPlayersUpdatedData> CreateRoomPlayersUpdatedEvent(string roomCode, List<GamePlayer> players, int maxPlayers = 10)
    {
        var playerInfos = players.Select(p => new PlayerInfo
        {
            UserId = p.UserId,
            Username = p.Username,
            IsHost = p.IsHost,
            IsReady = p.Status == "ready",
            Score = p.Score,
            JoinTime = p.JoinTime
        }).ToList();
        var host = playerInfos.FirstOrDefault(p => p.IsHost);
        var data = new RoomPlayersUpdatedData
        {
            RoomCode = roomCode,
            Players = playerInfos,
            TotalPlayers = players.Count,
            MaxPlayers = maxPlayers,
            Host = host
        };
        return new WebSocketMessage<RoomPlayersUpdatedData>(RoomManagementConstants.Events.RoomPlayersUpdated, data);
    }
    /// <summary>
    /// Tạo host-changed event
    /// </summary>
    public static WebSocketMessage<HostChangedData> CreateHostChangedEvent(string roomCode, GamePlayer newHost, GamePlayer? oldHost = null)
    {
        var newHostInfo = new PlayerInfo
        {
            UserId = newHost.UserId,
            Username = newHost.Username,
            IsHost = true,
            IsReady = newHost.Status == "ready",
            Score = newHost.Score,
            JoinTime = newHost.JoinTime
        };
        PlayerInfo? oldHostInfo = null;
        if (oldHost != null)
        {
            oldHostInfo = new PlayerInfo
            {
                UserId = oldHost.UserId,
                Username = oldHost.Username,
                IsHost = false,
                IsReady = oldHost.Status == "ready",
                Score = oldHost.Score,
                JoinTime = oldHost.JoinTime
            };
        }
        var data = new HostChangedData
        {
            RoomCode = roomCode,
            NewHost = newHostInfo,
            OldHost = oldHostInfo
        };
        return new WebSocketMessage<HostChangedData>(RoomManagementConstants.Events.HostChanged, data);
    }
    /// <summary>
    /// Tạo WebSocket message với anonymous object (fallback)
    /// </summary>
    public static WebSocketMessage<object> CreateGenericEvent(string eventType, object data)
    {
        return new WebSocketMessage<object>(eventType, data);
    }
}
