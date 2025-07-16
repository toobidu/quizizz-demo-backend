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
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    public RoomPlayer() { }

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