using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class CreateRoomRequestMapper
{
    public static Room ToEntity(CreateRoomRequest request, int ownerId)
    {
        return new Room(
            id: 0,
            roomCode: Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper(),
            roomName: request.Name,
            isPrivate: request.IsPrivate,
            ownerId: ownerId,
            maxPlayers: request.MaxPlayers
        );
    }
}