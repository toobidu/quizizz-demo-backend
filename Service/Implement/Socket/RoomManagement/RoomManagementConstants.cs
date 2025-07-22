namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;
/// <summary>
/// Constants cho Room Management Service
/// </summary>
public static class RoomManagementConstants
{
    /// <summary>
    /// WebSocket event names
    /// </summary>
    public static class Events
    {
        public const string RoomJoined = "room-joined";
        public const string RoomPlayersUpdated = "room-players-updated";
        public const string HostChanged = "host-changed";
        public const string PlayerLeft = "player-left";
        public const string RoomDeleted = "room-deleted";
    }
    /// <summary>
    /// Game states
    /// </summary>
    public static class GameStates
    {
        public const string Lobby = "lobby";
        public const string Waiting = "waiting";
        public const string Playing = "playing";
        public const string Finished = "finished";
    }
    /// <summary>
    /// Player statuses
    /// </summary>
    public static class PlayerStatuses
    {
        public const string Online = "online";
        public const string Offline = "offline";
        public const string Waiting = "waiting";
        public const string Playing = "playing";
    }
    /// <summary>
    /// Room limits
    /// </summary>
    public static class Limits
    {
        public const int MaxPlayersPerRoom = 10;
        public const int MinRoomCodeLength = 4;
        public const int MaxRoomCodeLength = 10;
        public const int MinUsernameLength = 2;
        public const int MaxUsernameLength = 50;
    }
    /// <summary>
    /// Messages
    /// </summary>
    public static class Messages
    {
        public const string RoomFull = "Phòng đã đầy";
        public const string GameInProgress = "Game đang diễn ra, không thể tham gia";
        public const string InvalidRoomCode = "Mã phòng không hợp lệ";
        public const string InvalidUsername = "Tên người chơi không hợp lệ";
        public const string InvalidUserId = "ID người chơi không hợp lệ";
        public const string InvalidSocket = "Socket connection không hợp lệ";
        public const string PlayerNotFound = "Player không tồn tại trong phòng";
        public const string RoomNotFound = "Phòng không tồn tại";
        public const string ConnectionUpdated = "Cập nhật kết nối thành công";
        public const string PlayerLeft = "Player đã rời phòng";
    }
}
