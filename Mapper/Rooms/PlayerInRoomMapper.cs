using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Mapper.Rooms;
public static class PlayerInRoomMapper
{
    public static PlayerInRoomDTO ToDTO(RoomPlayer rp, string username)
    {
        return new PlayerInRoomDTO(
            userId: rp.UserId,
            username: username,
            score: rp.Score,
            timeTaken: rp.TimeTaken
        );
    }
    public static RoomPlayer ToEntity(PlayerInRoomDTO dto)
    {
        return new RoomPlayer(
            roomId: 0,
            userId: dto.UserId,
            score: dto.Score,
            timeTaken: dto.TimeTaken,
            createdAt: DateTime.UtcNow, 
            updatedAt: DateTime.UtcNow 
        );
    }
}
