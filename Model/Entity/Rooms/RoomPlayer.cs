using System.ComponentModel.DataAnnotations.Schema;
using Dapper;

namespace ConsoleApp1.Model.Entity.Rooms;

public class RoomPlayer
{
    static RoomPlayer()
    {
        SqlMapper.SetTypeMap(typeof(RoomPlayer), new CustomPropertyTypeMap(
            typeof(RoomPlayer), (type, columnName) =>
                type.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName) ||
                    prop.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))));
    }
    
    [Column("room_id")]
    public int RoomId { get; set; }
    
    [Column("user_id")]
    public int UserId { get; set; }
    
    public int Score { get; set; }
    
    [Column("time_taken")]
    public TimeSpan TimeTaken { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "waiting";
    
    [Column("socket_id")]
    public string SocketId { get; set; }
    
    [Column("last_activity")]
    public DateTime? LastActivity { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public RoomPlayer() { }

    public RoomPlayer(int roomId, int userId, int score, TimeSpan timeTaken, 
                     string status, string socketId, DateTime? lastActivity,
                     DateTime createdAt, DateTime updatedAt)
    {
        RoomId = roomId;
        UserId = userId;
        Score = score;
        TimeTaken = timeTaken;
        Status = status;
        SocketId = socketId;
        LastActivity = lastActivity;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
    
    public RoomPlayer(int roomId, int userId, int score, TimeSpan timeTaken, 
                     DateTime createdAt, DateTime updatedAt)
    {
        RoomId = roomId;
        UserId = userId;
        Score = score;
        TimeTaken = timeTaken;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}