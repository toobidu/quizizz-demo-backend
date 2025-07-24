using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Mapper.Rooms;
public static class PlayerInRoomMapper
{
    public static PlayerInRoomDTO ToDTO(RoomPlayer rp, string username)
    {
        return new PlayerInRoomDTO
        {
            UserId = rp.UserId,
            Username = username,
            Score = rp.Score,
            TimeTaken = rp.TimeTaken,
            Status = rp.Status,
            SocketId = rp.SocketId,
            LastActivity = rp.LastActivity
        };
    }
    public static RoomPlayer ToEntity(PlayerInRoomDTO dto)
    {
        return new RoomPlayer
        {
            UserId = dto.UserId,
            Score = dto.Score,
            TimeTaken = dto.TimeTaken,
            Status = dto.Status,
            SocketId = dto.SocketId,
            LastActivity = dto.LastActivity,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
