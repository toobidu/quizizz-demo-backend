using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;

namespace ConsoleApp1.Mapper.Rooms;

public static class JoinRoomRequestMapper
{
    public static Room ToEntity(JoinRoomRequest request, Room room)
    {
        return room;
    }
}