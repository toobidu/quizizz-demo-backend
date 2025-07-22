using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
namespace ConsoleApp1.Service.Interface;
public interface ICreateRoomService
{
    Task<RoomDTO> CreateRoomAsync(CreateRoomRequest request, int userId);
    Task<bool> UpdateRoomSettingsAsync(int roomId, RoomSetting settings);
    Task<bool> KickPlayerAsync(int roomId, int playerId);
    Task<bool> UpdateRoomStatusAsync(int roomId, string status);
    Task<bool> SetTopicForRoomAsync(int roomId, int topicId);
    Task<bool> SetQuestionCountAsync(int roomId, int count);
    Task<bool> SetCountdownTimeAsync(int roomId, int seconds);
    Task<bool> UpdateRoomPrivacyAsync(int roomId, bool isPrivate);
    Task<bool> SetGameModeAsync(int roomId, string gameMode);
    Task<bool> UpdateMaxPlayersAsync(int roomId, int maxPlayers);
    Task<bool> DeleteRoomAsync(int roomId);
    Task<bool> TransferOwnershipAsync(int roomId, int newOwnerId);
}
