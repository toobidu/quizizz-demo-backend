using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ConsoleApp1.Service.Implement.Socket.RoomManagement;

/// <summary>
/// Service quản lý rooms và players
/// </summary>
public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, string> _socketToRoom;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private readonly RoomValidator _validator;

    public RoomManager(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, string> socketToRoom,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _socketToRoom = socketToRoom;
        _connections = connections;
        _validator = new RoomValidator();
    }

    /// <summary>
    /// Tạo hoặc lấy room
    /// </summary>
    public GameRoom GetOrCreateRoom(string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode))
        {
            _gameRooms[roomCode] = new GameRoom { RoomCode = roomCode };
            Console.WriteLine($"[ROOM] Created new game room: {roomCode}");
        }
        return _gameRooms[roomCode];
    }

    /// <summary>
    /// Thêm player vào room
    /// </summary>
    public (bool Success, string Message, GamePlayer? Player) AddPlayerToRoom(
        string roomCode, string socketId, string username, int userId)
    {
        // Validate inputs
        if (!_validator.IsValidRoomCode(roomCode))
            return (false, "Mã phòng không hợp lệ", null);

        if (!_validator.IsValidUsername(username))
            return (false, "Tên người chơi không hợp lệ", null);

        if (!_validator.IsValidUserId(userId))
            return (false, "ID người chơi không hợp lệ", null);

        if (!_validator.IsValidSocketId(socketId))
            return (false, "Socket connection không hợp lệ", null);

        var gameRoom = GetOrCreateRoom(roomCode);

        // Kiểm tra có thể join không
        var (canJoin, reason) = _validator.CanJoinRoom(gameRoom, userId);
        if (!canJoin && !_validator.IsPlayerExistsInRoom(gameRoom, userId))
        {
            return (false, reason, null);
        }

        // Lưu mapping socketId -> roomCode
        _socketToRoom[socketId] = roomCode;

        // Kiểm tra player đã tồn tại
        var existingPlayer = gameRoom.Players.FirstOrDefault(p => p.UserId == userId);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[ROOM] Người chơi {username} (ID: {userId}) đã tồn tại, đang cập nhật socket");
            existingPlayer.SocketId = socketId;
            return (true, "Cập nhật kết nối thành công", existingPlayer);
        }

        // Tạo player mới
        var isFirstPlayer = gameRoom.Players.Count == 0;
        var player = new GamePlayer
        {
            Username = username,
            UserId = userId,
            SocketId = socketId,
            IsHost = isFirstPlayer,
            JoinTime = DateTime.UtcNow
        };

        gameRoom.Players.Add(player);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] WEBSOCKET JOIN - Room {roomCode}: {username} (ID: {userId}) joined as {(player.IsHost ? "HOST" : "player")}");
        Console.WriteLine($"[{timestamp}] WEBSOCKET ROOM STATUS - Room {roomCode}: {gameRoom.Players.Count} players connected | Host: {gameRoom.Players.FirstOrDefault(p => p.IsHost)?.Username}");

        return (true, $"Chào mừng {username} đến phòng {roomCode}!", player);
    }

    /// <summary>
    /// Xóa player khỏi room
    /// </summary>
    public (bool Success, string Message, GamePlayer? RemovedPlayer, GamePlayer? NewHost) RemovePlayerFromRoom(
        string socketId, string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode))
            return (false, "Phòng không tồn tại", null, null);

        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.SocketId == socketId);

        if (player == null)
            return (false, "Player không tồn tại trong phòng", null, null);

        // Xóa player
        gameRoom.Players.Remove(player);
        _socketToRoom.TryRemove(socketId, out _);

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] WEBSOCKET LEAVE - Room {roomCode}: {player.Username} (ID: {player.UserId}) left");

        GamePlayer? newHost = null;

        // Xử lý chuyển host nếu cần
        if (player.IsHost && gameRoom.Players.Count > 0)
        {
            newHost = gameRoom.Players.OrderBy(p => p.JoinTime ?? DateTime.MaxValue).First();
            newHost.IsHost = true;
            Console.WriteLine($"[{timestamp}] HOST TRANSFER - Room {roomCode}: {newHost.Username} is now the host");
        }

        // Xóa room nếu không còn ai
        if (gameRoom.Players.Count == 0)
        {
            _gameRooms.TryRemove(roomCode, out _);
            Console.WriteLine($"[{timestamp}] ROOM DELETED - Room {roomCode}: No players remaining");
            Console.WriteLine($"[ROOM] Game session cleanup needed for empty room {roomCode}");
        }

        return (true, "Player đã rời phòng", player, newHost);
    }

    /// <summary>
    /// Lấy thông tin players trong room
    /// </summary>
    public List<RoomPlayerInfo> GetRoomPlayers(string roomCode)
    {
        if (!_gameRooms.TryGetValue(roomCode, out var gameRoom))
            return new List<RoomPlayerInfo>();

        return gameRoom.Players.Select(p => new RoomPlayerInfo
        {
            Username = p.Username,
            UserId = p.UserId,
            IsHost = p.IsHost,
            JoinTime = p.JoinTime,
            IsOnline = _connections.ContainsKey(p.SocketId ?? "") &&
                      _connections.TryGetValue(p.SocketId ?? "", out var ws) &&
                      ws.State == WebSocketState.Open
        }).ToList();
    }

    /// <summary>
    /// Kiểm tra room có tồn tại không
    /// </summary>
    public bool RoomExists(string roomCode)
    {
        return _gameRooms.ContainsKey(roomCode);
    }

    /// <summary>
    /// Lấy room
    /// </summary>
    public GameRoom? GetRoom(string roomCode)
    {
        _gameRooms.TryGetValue(roomCode, out var room);
        return room;
    }

    /// <summary>
    /// Lấy tổng số rooms
    /// </summary>
    public int GetTotalRooms()
    {
        return _gameRooms.Count;
    }

    /// <summary>
    /// Lấy tổng số players online
    /// </summary>
    public int GetTotalPlayersOnline()
    {
        return _gameRooms.Values.Sum(room => room.Players.Count);
    }
}