using System.ComponentModel.DataAnnotations.Schema;
using Dapper;
namespace ConsoleApp1.Model.Entity.Rooms;
public class Room
{
    static Room()
    {
        SqlMapper.SetTypeMap(typeof(Room), new CustomPropertyTypeMap(
            typeof(Room), (type, columnName) =>
                type.GetProperties().FirstOrDefault(prop =>
                    prop.GetCustomAttributes(false)
                        .OfType<ColumnAttribute>()
                        .Any(attr => attr.Name == columnName) ||
                    prop.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase))));
    }
    public int Id { get; set; }
    [Column("room_code")]
    public string RoomCode { get; set; } = string.Empty;
    [Column("room_name")]
    public string RoomName { get; set; } = string.Empty;
    [Column("is_private")]
    public bool IsPrivate { get; set; }
    [Column("owner_id")]
    public int OwnerId { get; set; }
    
    // Alias for OwnerId to maintain compatibility
    public int HostUserId { 
        get { return OwnerId; }
        set { OwnerId = value; }
    }
    public string Status { get; set; } = string.Empty;
    [Column("max_players")]
    public int MaxPlayers { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
    public Room() { }
    public Room(int id, string roomCode, string roomName, bool isPrivate, 
        int ownerId, string status, int maxPlayers, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        RoomCode = roomCode;
        RoomName = roomName;
        IsPrivate = isPrivate;
        OwnerId = ownerId;
        Status = status;
        MaxPlayers = maxPlayers;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}
