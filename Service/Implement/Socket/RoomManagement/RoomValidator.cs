using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;

namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;

/// <summary>
/// Service validation cho Room Management
/// </summary>
public class RoomValidator
{
    /// <summary>
    /// Validate room code format
    /// </summary>
    public bool IsValidRoomCode(string roomCode)
    {
        return !string.IsNullOrWhiteSpace(roomCode) && 
               roomCode.Length >= 4 && 
               roomCode.Length <= 10;
    }

    /// <summary>
    /// Validate username
    /// </summary>
    public bool IsValidUsername(string username)
    {
        return !string.IsNullOrWhiteSpace(username) && 
               username.Length >= 2 && 
               username.Length <= 50;
    }

    /// <summary>
    /// Validate user ID
    /// </summary>
    public bool IsValidUserId(int userId)
    {
        return userId > 0;
    }

    /// <summary>
    /// Kiểm tra xem player đã tồn tại trong room chưa
    /// </summary>
    public bool IsPlayerExistsInRoom(GameRoom gameRoom, int userId)
    {
        return gameRoom.Players.Any(p => p.UserId == userId);
    }

    /// <summary>
    /// Kiểm tra xem room có đầy không
    /// </summary>
    public bool IsRoomFull(GameRoom gameRoom, int maxPlayers = 10)
    {
        return gameRoom.Players.Count >= maxPlayers;
    }

    /// <summary>
    /// Kiểm tra xem room có thể join được không
    /// </summary>
    public (bool CanJoin, string Reason) CanJoinRoom(GameRoom gameRoom, int userId, int maxPlayers = 10)
    {
        if (IsRoomFull(gameRoom, maxPlayers))
        {
            return (false, "Phòng đã đầy");
        }

        if (gameRoom.GameState == "playing")
        {
            return (false, "Game đang diễn ra, không thể tham gia");
        }

        if (IsPlayerExistsInRoom(gameRoom, userId))
        {
            return (true, "Player đã tồn tại, cập nhật connection");
        }

        return (true, "Có thể tham gia");
    }

    /// <summary>
    /// Validate socket connection
    /// </summary>
    public bool IsValidSocketId(string socketId)
    {
        return !string.IsNullOrWhiteSpace(socketId);
    }
}