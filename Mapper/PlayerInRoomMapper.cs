using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class PlayerInRoomMapper
{
    public static PlayerInRoomDTO ToDTO(RoomPlayer roomPlayer, string username)
    {
        return new PlayerInRoomDTO(
            userId: roomPlayer.UserId,
            username: username,
            score: roomPlayer.Score,
            timeTaken: roomPlayer.TimeTaken
        );
    }

    public static RoomPlayer ToEntity(PlayerInRoomDTO playerDto)
    {
        return new RoomPlayer(
            roomId: 0, 
            userId: playerDto.UserId,
            score: playerDto.Score,
            timeTaken: playerDto.TimeTaken
        );
    }
}