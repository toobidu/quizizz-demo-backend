using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class RoomMapper
{
    public static RoomDTO ToDTO(Room room)
    {
        return new RoomDTO(
            code: room.RoomCode,
            name: room.RoomName,
            isPrivate: room.IsPrivate,
            ownerId: room.OwnerId
        );
    }

    public static Room ToEntity(RoomDTO roomDto)
    {
        return new Room(
            id: 0, 
            roomCode: roomDto.Code,
            roomName: roomDto.Name,
            isPrivate: roomDto.IsPrivate,
            ownerId: roomDto.OwnerId
        );
    }

    public static RoomSummaryDTO ToSummaryDTO(Room room, int playerCount)
    {
        return new RoomSummaryDTO(
            roomCode: room.RoomCode,
            roomName: room.RoomName,
            isPrivate: room.IsPrivate,
            playerCount: playerCount
        );
    }
}