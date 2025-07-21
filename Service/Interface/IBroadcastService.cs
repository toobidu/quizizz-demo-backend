using ConsoleApp1.Model.DTO.Rooms;

namespace ConsoleApp1.Service.Interface;

public interface IBroadcastService
{
    /// <summary>
    /// Broadcast cập nhật danh sách người chơi trong phòng
    /// Gửi qua cả WebSocket và HTTP polling
    /// </summary>
    Task BroadcastRoomPlayersUpdateAsync(string roomCode);
    
    /// <summary>
    /// Broadcast sự kiện khi có người chơi mới tham gia phòng
    /// Gửi event 'player-joined' cho các người chơi khác trong phòng
    /// </summary>
    Task BroadcastPlayerJoinedAsync(string roomCode, int newPlayerId);
    
    /// <summary>
    /// Broadcast khi có phòng mới được tạo
    /// </summary>
    Task BroadcastRoomCreatedAsync(RoomDTO room);
    
    /// <summary>
    /// Broadcast khi phòng bị xóa
    /// </summary>
    Task BroadcastRoomDeletedAsync(string roomCode);
    
    /// <summary>
    /// Broadcast cập nhật danh sách tất cả phòng (cho lobby)
    /// </summary>
    Task BroadcastRoomsListUpdateAsync();
    
    /// <summary>
    /// Broadcast sự kiện đồng bộ khi có người chơi tham gia phòng qua HTTP API
    /// Gửi event 'sync-room-join' để frontend biết cần gửi WebSocket joinRoom
    /// </summary>
    Task BroadcastSyncRoomJoinAsync(string roomCode, int userId, string username);
}