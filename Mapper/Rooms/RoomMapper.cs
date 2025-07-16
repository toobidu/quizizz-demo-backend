using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Mapper.Rooms;

public static class RoomMapper
{
    public static RoomDTO ToDTO(Room room)
    {
        return new RoomDTO(
            id: room.Id,
            code: room.RoomCode,
            name: room.RoomName,
            isPrivate: room.IsPrivate,
            ownerId: room.OwnerId,
            maxPlayers: room.MaxPlayers,
            status: room.Status
        );
    }

    public static Room ToEntity(RoomDTO roomDto)
    {
        return new Room(
            id: 0,
            roomCode: roomDto.Code,
            roomName: roomDto.Name,
            isPrivate: roomDto.IsPrivate,
            ownerId: roomDto.OwnerId,
            status: roomDto.Status,
            maxPlayers: roomDto.MaxPlayers,
            createdAt: DateTime.UtcNow, 
            updatedAt: DateTime.UtcNow  
        );
    }

    public static RoomSummaryDTO ToSummaryDTO(Room room, int playerCount, string? topicName = null, int questionCount = 0, int countdownTime = 0)
    {
        return new RoomSummaryDTO(
            roomCode: room.RoomCode,
            roomName: room.RoomName,
            isPrivate: room.IsPrivate,
            playerCount: playerCount,
            maxPlayers: room.MaxPlayers,
            status: room.Status,
            topicName: topicName,
            questionCount: questionCount,
            countdownTime: countdownTime
        );
    }
}