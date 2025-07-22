using ConsoleApp1.Model.DTO.Rooms;
namespace ConsoleApp1.Extensions;
public static class CreateRoomRequestExtensions
{
    public static bool IsValid(this CreateRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) return false;
        if (request.GameMode == "1vs1" && request.MaxPlayers != 2) return false;
        if (request.GameMode == "battle" && request.MaxPlayers < 3) return false;
        if (request.GameMode != "1vs1" && request.GameMode != "battle") return false;
        return request.MaxPlayers > 0 && request.MaxPlayers <= 50;
    }
}
