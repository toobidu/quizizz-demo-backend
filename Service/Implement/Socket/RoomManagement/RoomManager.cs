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
            var newRoom = new GameRoom { RoomCode = roomCode };
            _gameRooms[roomCode] = newRoom;
            Console.WriteLine($"🏗️ [RoomManager] Created NEW room {roomCode}. Hash: {newRoom.GetHashCode()}. Total rooms: {_gameRooms.Count}");
        }
        else
        {
            Console.WriteLine($"♻️ [RoomManager] Retrieved EXISTING room {roomCode}. Hash: {_gameRooms[roomCode].GetHashCode()}. Players: {_gameRooms[roomCode].Players.Count}");
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
        
        // ✅ THÊM LOGGING CHI TIẾT CHO JOIN-ROOM
        Console.WriteLine($"🔗 [RoomManager] Mapped socketId '{socketId}' to room '{roomCode}'. Total mappings: {_socketToRoom.Count}");
        Console.WriteLine($"🔍 [RoomManager] Current _socketToRoom mappings: [{string.Join(", ", _socketToRoom.Select(kv => $"{kv.Key}→{kv.Value}"))}]");
        // Kiểm tra player đã tồn tại
        var existingPlayer = gameRoom.Players.FirstOrDefault(p => p.UserId == userId);
        if (existingPlayer != null)
        {
            // Xóa mapping socketId cũ -> roomCode nếu có
            if (!string.IsNullOrEmpty(existingPlayer.SocketId))
            {
                _socketToRoom.TryRemove(existingPlayer.SocketId, out _);
                // Xóa khỏi _connections nếu cần
                if (_connections.ContainsKey(existingPlayer.SocketId))
                {
                    _connections.TryRemove(existingPlayer.SocketId, out _);
                }
            }
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
        
        // ✅ THÊM DETAILED LOGGING CHO PLAYER ADDITION
        Console.WriteLine($"✅ [RoomManager] Added player {username} (ID: {userId}) to room {roomCode}. Room now has {gameRoom.Players.Count} players");
        Console.WriteLine($"🎮 [RoomManager] Players in room {roomCode}: [{string.Join(", ", gameRoom.Players.Select(p => $"{p.Username}({p.UserId})"))}]");
        Console.WriteLine($"🎮 [RoomManager] Game room object hash: {gameRoom.GetHashCode()}");
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        return (true, $"Chào mừng {username} đến phòng {roomCode}!", player);
    }
    /// <summary>
    /// Xóa player khỏi room
    /// </summary>
    public (bool Success, string Message, GamePlayer? RemovedPlayer, GamePlayer? NewHost) RemovePlayerFromRoom(
        string socketId, string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode))
        {
            return (false, "Phòng không tồn tại", null, null);
        }
        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.SocketId == socketId);
        if (player == null)
        {
            return (false, "Player không tồn tại trong phòng", null, null);
        }
        // Lưu thông tin người chơi trước khi xóa
        var playerToRemove = new GamePlayer
        {
            UserId = player.UserId,
            Username = player.Username,
            SocketId = player.SocketId,
            IsHost = player.IsHost,
            Score = player.Score,
            Status = player.Status,
            JoinTime = player.JoinTime
        };
        // Xóa player
        gameRoom.Players.Remove(player);
        
        // ✅ THÊM LOGGING CHI TIẾT CHO LEAVE-ROOM
        bool removed = _socketToRoom.TryRemove(socketId, out var removedRoomCode);
        Console.WriteLine($"🔗 [RoomManager] Removed socketId '{socketId}' from room '{removedRoomCode}'. Success: {removed}. Total mappings: {_socketToRoom.Count}");
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        GamePlayer? newHost = null;
        // Xử lý chuyển host nếu cần
        if (playerToRemove.IsHost && gameRoom.Players.Count > 0)
        {
            newHost = gameRoom.Players.OrderBy(p => p.JoinTime ?? DateTime.MaxValue).First();
            newHost.IsHost = true;
        }
        // Xóa room nếu không còn ai
        if (gameRoom.Players.Count == 0)
        {
            _gameRooms.TryRemove(roomCode, out _);
        }
        else
        {
            // Log danh sách người chơi còn lại
        }
        return (true, "Player đã rời phòng", playerToRemove, newHost);
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
    /// Xóa player khỏi room theo userId thay vì socketId
    /// Sử dụng khi cần xóa người chơi dựa trên userId (ví dụ: khi gọi từ HTTP API)
    /// </summary>
    public (bool Success, string Message, GamePlayer? RemovedPlayer, GamePlayer? NewHost) RemovePlayerFromRoomByUserId(
        int userId, string roomCode)
    {
        if (!_gameRooms.ContainsKey(roomCode))
        {
            return (false, "Phòng không tồn tại", null, null);
        }
        var gameRoom = _gameRooms[roomCode];
        var player = gameRoom.Players.FirstOrDefault(p => p.UserId == userId);
        if (player == null)
        {
            return (false, "Player không tồn tại trong phòng", null, null);
        }
        // Lưu thông tin người chơi trước khi xóa
        var playerToRemove = new GamePlayer
        {
            UserId = player.UserId,
            Username = player.Username,
            SocketId = player.SocketId,
            IsHost = player.IsHost,
            Score = player.Score,
            Status = player.Status,
            JoinTime = player.JoinTime
        };
        // Xóa mapping socketId -> roomCode nếu có
        if (!string.IsNullOrEmpty(player.SocketId))
        {
            _socketToRoom.TryRemove(player.SocketId, out _);
        }
        // Xóa player
        gameRoom.Players.Remove(player);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        GamePlayer? newHost = null;
        // Xử lý chuyển host nếu cần
        if (playerToRemove.IsHost && gameRoom.Players.Count > 0)
        {
            newHost = gameRoom.Players.OrderBy(p => p.JoinTime ?? DateTime.MaxValue).First();
            newHost.IsHost = true;
        }
        // Xóa room nếu không còn ai
        if (gameRoom.Players.Count == 0)
        {
            _gameRooms.TryRemove(roomCode, out _);
        }
        else
        {
            // Log danh sách người chơi còn lại
        }
        return (true, "Player đã rời phòng", playerToRemove, newHost);
    }
    /// <summary>
    /// Lấy room - sẽ tạo mới nếu chưa tồn tại
    /// </summary>
    public GameRoom? GetRoom(string roomCode)
    {
        // ✅ SỬA: Sử dụng GetOrCreateRoom để đảm bảo room luôn tồn tại khi cần
        Console.WriteLine($"🏠 [RoomManager] Getting room {roomCode}. Total rooms in memory: {_gameRooms.Count}");
        var room = GetOrCreateRoom(roomCode);
        Console.WriteLine($"🏠 [RoomManager] Room {roomCode} - Players: {room.Players.Count}");
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
