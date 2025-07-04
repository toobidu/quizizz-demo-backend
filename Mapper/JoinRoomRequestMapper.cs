using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class JoinRoomRequestMapper
{
    public static Room ToEntity(JoinRoomRequest request, Room room)
    {
        return room;
    }
}