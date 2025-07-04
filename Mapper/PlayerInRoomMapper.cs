using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

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
            timeTaken: dto.TimeTaken
        );
    }
}