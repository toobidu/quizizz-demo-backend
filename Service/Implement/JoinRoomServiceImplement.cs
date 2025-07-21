using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using ConsoleApp1.Mapper.Rooms;
using System.Collections.Concurrent;

namespace ConsoleApp1.Service.Implement;

public class JoinRoomServiceImplement : IJoinRoomService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ICreateRoomService _createRoomService;
    private readonly ISocketService _socketService;
    public readonly IBroadcastService _broadcastService;

    public JoinRoomServiceImplement(
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        ICreateRoomService createRoomService,
        ISocketService socketService,
        IBroadcastService broadcastService)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _createRoomService = createRoomService;
        _socketService = socketService;
        _broadcastService = broadcastService;
    }

    public async Task<RoomDTO?> JoinPublicRoomAsync(int roomId, int playerId)
    {
        Console.WriteLine($"[Dịch vụ-ThamGiaPhòng] JoinPublicRoomAsync được gọi - roomId: {roomId}, playerId: {playerId}");
        
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            Console.WriteLine($"[Dịch vụ-ThamGiaPhòng] Không tìm thấy phòng {roomId}");
            return null;
        }
        
        Console.WriteLine($"[Dịch vụ-ThamGiaPhòng] Tìm thấy phòng - Tên: {room.RoomName}, Riêng tư: {room.IsPrivate}, Trạng thái: {room.Status}");
        
        if (room.IsPrivate || room.Status != "waiting")
        {
            Console.WriteLine($"[JoinRoomService] Cannot join room - IsPrivate: {room.IsPrivate}, Status: {room.Status}");
            return null;
        }

        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        Console.WriteLine($"[JoinRoomService] Current player count: {playerCount}/{room.MaxPlayers}");
        
        if (playerCount >= room.MaxPlayers)
        {
            Console.WriteLine($"[JoinRoomService] Room is full, updating status");
            await _roomRepository.UpdateStatusAsync(roomId, "full");
            return null;
        }

        var existingPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(playerId, roomId);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[JoinRoomService] Player {playerId} already in room {roomId}");
            return RoomMapper.ToDTO(room);
        }
        
        var activeRoom = await _roomPlayerRepository.GetActiveRoomByUserIdAsync(playerId);
        if (activeRoom != null)
        {
            Console.WriteLine($"[JoinRoomService] Player {playerId} is in another room {activeRoom.RoomCode}, removing them first");
            await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, activeRoom.Id);
            
            // Gửi sự kiện rời phòng qua WebSocket
            try 
            {
                await _socketService.LeaveRoomByUserIdAsync(playerId, activeRoom.RoomCode);
                Console.WriteLine($"[JoinRoomService] Sent leave room event for player {playerId} from room {activeRoom.RoomCode}");
                
                // Đợi một chút để đảm bảo sự kiện rời phòng được xử lý
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JoinRoomService] Error sending leave room event: {ex.Message}");
            }
        }

        Console.WriteLine($"[JoinRoomService] Adding player {playerId} to room {roomId}");
        var roomPlayer = new RoomPlayer(roomId, playerId, 0, TimeSpan.Zero, DateTime.UtcNow, DateTime.UtcNow);
        await _roomPlayerRepository.AddAsync(roomPlayer);

        var newPlayerCount = playerCount + 1;
        Console.WriteLine($"[JoinRoomService] New player count: {newPlayerCount}");
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        if (newPlayerCount >= room.MaxPlayers)
        {
            Console.WriteLine($"[{timestamp}] 🔄 ROOM STATUS CHANGED - Room {room.RoomCode}: waiting → ready (FULL)");
            room = await _roomRepository.UpdateStatusAsync(roomId, "ready");
        }

        var finalPlayerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        Console.WriteLine($"[{timestamp}] ✅ PLAYER JOINED - Room {room.RoomCode} (ID: {roomId}): Player {playerId} joined successfully");
        Console.WriteLine($"[{timestamp}] 📊 WAITING ROOM STATUS - Room {room.RoomCode}: {finalPlayerCount}/{room.MaxPlayers} players | Status: {room.Status}");
        
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            // Broadcast sự kiện player-joined cho các người chơi khác
            await _broadcastService.BroadcastPlayerJoinedAsync(room.RoomCode, playerId);
            // Broadcast cập nhật danh sách người chơi
            await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
            Console.WriteLine($"[{timestamp}] 📡 BROADCAST - Room {room.RoomCode}: Player join broadcasted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{timestamp}] ⚠️ BROADCAST ERROR - Room {room.RoomCode}: {ex.Message}");
        }
        
        return RoomMapper.ToDTO(room);
    }

    public async Task<RoomDTO?> JoinPrivateRoomAsync(string roomCode, int playerId)
    {
        Console.WriteLine($"[JoinRoomService] JoinPrivateRoomAsync called - roomCode: {roomCode}, playerId: {playerId}");
        
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null)
        {
            Console.WriteLine($"[JoinRoomService] Room {roomCode} not found");
            return null;
        }
        
        Console.WriteLine($"[JoinRoomService] Room found - Name: {room.RoomName}, IsPrivate: {room.IsPrivate}, Status: {room.Status}");
        
        if (room.Status != "waiting")
        {
            Console.WriteLine($"[JoinRoomService] Cannot join room - Status: {room.Status} (must be 'waiting')");
            return null;
        }

        var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
        Console.WriteLine($"[JoinRoomService] Current player count: {playerCount}/{room.MaxPlayers}");
        
        if (playerCount >= room.MaxPlayers)
        {
            Console.WriteLine($"[JoinRoomService] Room is full, updating status");
            await _roomRepository.UpdateStatusAsync(room.Id, "full");
            return null;
        }

        var existingPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(playerId, room.Id);
        if (existingPlayer != null)
        {
            Console.WriteLine($"[JoinRoomService] Player {playerId} already in room {room.RoomCode}");
            return RoomMapper.ToDTO(room);
        }
        
        var activeRoom = await _roomPlayerRepository.GetActiveRoomByUserIdAsync(playerId);
        if (activeRoom != null)
        {
            Console.WriteLine($"[JoinRoomService] Player {playerId} is in another room {activeRoom.RoomCode}, removing them first");
            await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, activeRoom.Id);
            
            // Gửi sự kiện rời phòng qua WebSocket
            try 
            {
                await _socketService.LeaveRoomByUserIdAsync(playerId, activeRoom.RoomCode);
                Console.WriteLine($"[JoinRoomService] Sent leave room event for player {playerId} from room {activeRoom.RoomCode}");
                
                // Đợi một chút để đảm bảo sự kiện rời phòng được xử lý
                await Task.Delay(200);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[JoinRoomService] Error sending leave room event: {ex.Message}");
            }
        }

        Console.WriteLine($"[JoinRoomService] Adding player {playerId} to room {room.RoomCode}");
        var roomPlayer = new RoomPlayer(room.Id, playerId, 0, TimeSpan.Zero, DateTime.UtcNow, DateTime.UtcNow);
        await _roomPlayerRepository.AddAsync(roomPlayer);

        var newPlayerCount = playerCount + 1;
        Console.WriteLine($"[JoinRoomService] New player count: {newPlayerCount}");
        
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        if (newPlayerCount >= room.MaxPlayers)
        {
            Console.WriteLine($"[{timestamp}] 🔄 ROOM STATUS CHANGED - Room {room.RoomCode}: waiting → ready (FULL)");
            room = await _roomRepository.UpdateStatusAsync(room.Id, "ready");
        }

        var finalPlayerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
        Console.WriteLine($"[{timestamp}] ✅ PLAYER JOINED - Room {room.RoomCode} (ID: {room.Id}): Player {playerId} joined successfully");
        Console.WriteLine($"[{timestamp}] 📊 WAITING ROOM STATUS - Room {room.RoomCode}: {finalPlayerCount}/{room.MaxPlayers} players | Status: {room.Status}");
        
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            // Broadcast sự kiện player-joined cho các người chơi khác
            await _broadcastService.BroadcastPlayerJoinedAsync(room.RoomCode, playerId);
            // Broadcast cập nhật danh sách người chơi
            await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
            Console.WriteLine($"[{timestamp}] 📡 BROADCAST - Room {room.RoomCode}: Player join broadcasted");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{timestamp}] ⚠️ BROADCAST ERROR - Room {room.RoomCode}: {ex.Message}");
        }
        
        return RoomMapper.ToDTO(room);
    }

    public async Task<bool> LeaveRoomAsync(int roomId, int playerId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] ❌ PLAYER LEAVING - Room {room.RoomCode} (ID: {roomId}): Player {playerId} attempting to leave");
        
        // Xóa player khỏi room_players TRƯỚC
        var deleteResult = await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(playerId, roomId);
        if (!deleteResult)
        {
            Console.WriteLine($"[{timestamp}] ⚠️ PLAYER NOT IN ROOM - Player {playerId} was not in room {roomId}");
            return false;
        }
        
        var playerCountAfter = await _roomRepository.GetPlayerCountAsync(roomId);
        Console.WriteLine($"[{timestamp}] 📊 PLAYER LEFT - Room {room.RoomCode}: {playerCountAfter}/{room.MaxPlayers} players remaining");

        // Cập nhật status nếu cần
        if (playerCountAfter < room.MaxPlayers && (room.Status == "full" || room.Status == "ready"))
        {
            await _roomRepository.UpdateStatusAsync(roomId, "waiting");
            Console.WriteLine($"[{timestamp}] 🔄 ROOM STATUS CHANGED - Room {room.RoomCode}: full/ready → waiting");
        }

        // Xử lý owner rời phòng SAU KHI đã xóa khỏi room_players
        if (room.OwnerId == playerId)
        {
            Console.WriteLine($"[{timestamp}] 👑 OWNER LEFT - Room {room.RoomCode}: Checking remaining players");
            
            if (playerCountAfter == 0)
            {
                Console.WriteLine($"[{timestamp}] 🗑️ ROOM DELETED - Room {room.RoomCode} (ID: {roomId}): No players remaining");
                await _createRoomService.DeleteRoomAsync(roomId);
            }
            else
            {
                // Chuyển quyền owner cho người chơi đầu tiên còn lại
                var remainingPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
                if (remainingPlayers.Any())
                {
                    var nextOwner = remainingPlayers.OrderBy(p => p.CreatedAt).First();
                    Console.WriteLine($"[{timestamp}] 🔄 OWNERSHIP TRANSFERRED - Room {room.RoomCode}: New owner is Player {nextOwner.UserId}");
                    await _createRoomService.TransferOwnershipAsync(roomId, nextOwner.UserId);
                }
            }
        }
        
        // Broadcast update qua cả WebSocket và HTTP
        try
        {
            var roomForBroadcast = await _roomRepository.GetByIdAsync(roomId);
            if (roomForBroadcast != null)
            {
                // Sử dụng phương thức LeaveRoomByUserIdAsync mới để xử lý rời phòng qua WebSocket
                await _socketService.LeaveRoomByUserIdAsync(playerId, roomForBroadcast.RoomCode);
                Console.WriteLine($"[{timestamp}] 📡 BROADCAST - Room {roomForBroadcast.RoomCode}: Player {playerId} left via WebSocket");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{timestamp}] ⚠️ BROADCAST ERROR - Leave broadcast: {ex.Message}");
            Console.WriteLine($"[{timestamp}] ⚠️ STACK TRACE: {ex.StackTrace}");
        }

        return true;
    }

    public async Task<bool> LeaveRoomByCodeAsync(string roomCode, int playerId)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        if (room == null) return false;
        
        return await LeaveRoomAsync(room.Id, playerId);
    }

    public async Task<RoomDTO?> GetRoomByCodeAsync(string roomCode)
    {
        var room = await _roomRepository.GetByCodeAsync(roomCode);
        return room != null ? RoomMapper.ToDTO(room) : null;
    }

    public async Task<IEnumerable<RoomSummaryDTO>> GetPublicRoomsAsync()
    {
        var rooms = await _roomRepository.GetPublicWaitingRoomsAsync();
        
        var result = new List<RoomSummaryDTO>();
        foreach (var room in rooms)
        {
            var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
            var topicName = await _roomRepository.GetRoomTopicNameAsync(room.Id);
            var questionCount = await _roomRepository.GetRoomQuestionCountAsync(room.Id);
            var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(room.Id);
            
            result.Add(new RoomSummaryDTO(room.RoomCode, room.RoomName, room.IsPrivate, 
                playerCount, room.MaxPlayers, room.Status, topicName, questionCount, countdownTime));
        }
        
        return result;
    }

    public async Task<IEnumerable<RoomSummaryDTO>> GetAllRoomsAsync()
    {
        Console.WriteLine("[JoinRoomService] GetAllRoomsAsync called");
        
        var rooms = await _roomRepository.GetAllRoomsWithDetailsAsync();
        Console.WriteLine($"[JoinRoomService] Repository returned {rooms?.Count() ?? 0} rooms");
        
        if (rooms == null)
        {
            Console.WriteLine("[JoinRoomService] Repository returned null, returning empty list");
            return new List<RoomSummaryDTO>();
        }
        
        var result = new List<RoomSummaryDTO>();
        foreach (var room in rooms)
        {
            Console.WriteLine($"[JoinRoomService] Processing room: Id={room.Id}, Name={room.RoomName}, Code={room.RoomCode}");
            
            var playerCount = await _roomRepository.GetPlayerCountAsync(room.Id);
            var topicName = await _roomRepository.GetRoomTopicNameAsync(room.Id);
            var questionCount = await _roomRepository.GetRoomQuestionCountAsync(room.Id);
            var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(room.Id);
            
            Console.WriteLine($"[JoinRoomService] Room details - Players: {playerCount}/{room.MaxPlayers}, Topic: {topicName}, Questions: {questionCount}");
            
            result.Add(new RoomSummaryDTO(room.RoomCode, room.RoomName, room.IsPrivate, 
                playerCount, room.MaxPlayers, room.Status, topicName, questionCount, countdownTime));
        }
        
        Console.WriteLine($"[JoinRoomService] Returning {result.Count} room summaries");
        return result;
    }

    // Dictionary để theo dõi thời gian gửi sự kiện cuối cùng cho mỗi phòng
    private readonly ConcurrentDictionary<string, DateTime> _lastGetPlayersTime = new();
    
    public async Task<IEnumerable<PlayerInRoomDTO>> GetPlayersInRoomAsync(int roomId)
    {
        Console.WriteLine($"[JoinRoomService] GetPlayersInRoomAsync called for roomId: {roomId}");
        
        var roomPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        Console.WriteLine($"[JoinRoomService] Found {roomPlayers?.Count() ?? 0} players in room {roomId}");
        
        var result = new List<PlayerInRoomDTO>();

        foreach (var rp in roomPlayers)
        {
            Console.WriteLine($"[JoinRoomService] Processing player - UserId: {rp.UserId}, Score: {rp.Score}");
            var user = await _userRepository.GetByIdAsync(rp.UserId);
            if (user != null)
            {
                Console.WriteLine($"[JoinRoomService] User found - Id: {user.Id}, Username: {user.Username}");
                result.Add(new PlayerInRoomDTO(user.Id, user.Username, rp.Score, rp.TimeTaken));
            }
            else
            {
                Console.WriteLine($"[JoinRoomService] User not found for UserId: {rp.UserId}");
            }
        }

        Console.WriteLine($"[JoinRoomService] Returning {result.Count} players for room {roomId}");
        
        // Tìm roomCode để gửi broadcast cập nhật
        try
        {
            var room = await _roomRepository.GetByIdAsync(roomId);
            if (room != null)
            {
                // Kiểm tra xem đã gửi sự kiện này gần đây chưa (trong vòng 2 giây)
                string cacheKey = $"get_players_{room.RoomCode}";
                if (_lastGetPlayersTime.TryGetValue(cacheKey, out var lastTime) && 
                    (DateTime.UtcNow - lastTime).TotalMilliseconds < 2000)
                {
                    Console.WriteLine($"[JoinRoomService] Skipping WebSocket update for room {room.RoomCode} (sent {(DateTime.UtcNow - lastTime).TotalMilliseconds}ms ago)");
                }
                else
                {
                    // Cập nhật thời gian gửi mới nhất
                    _lastGetPlayersTime[cacheKey] = DateTime.UtcNow;
                    
                    // Gửi broadcast cập nhật danh sách người chơi qua WebSocket
                    await _broadcastService.BroadcastRoomPlayersUpdateAsync(room.RoomCode);
                    Console.WriteLine($"[JoinRoomService] Triggered WebSocket update for room {room.RoomCode}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JoinRoomService] Error triggering WebSocket update: {ex.Message}");
        }
        
        return result;
    }

    public async Task<RoomDetailsDTO?> GetRoomDetailsAsync(int roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return null;

        var owner = await _userRepository.GetByIdAsync(room.OwnerId);
        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        var topicName = await _roomRepository.GetRoomTopicNameAsync(roomId);
        var questionCount = await _roomRepository.GetRoomQuestionCountAsync(roomId);
        var countdownTime = await _roomRepository.GetRoomCountdownTimeAsync(roomId);
        var players = await GetPlayersInRoomAsync(roomId);

        return new RoomDetailsDTO
        {
            Id = room.Id,
            RoomCode = room.RoomCode,
            RoomName = room.RoomName,
            IsPrivate = room.IsPrivate,
            OwnerId = room.OwnerId,
            OwnerUsername = owner?.Username ?? "Unknown",
            Status = room.Status,
            MaxPlayers = room.MaxPlayers,
            CurrentPlayerCount = playerCount,
            TopicName = topicName ?? "Kiến thức chung",
            QuestionCount = questionCount,
            CountdownTime = countdownTime,
            GameMode = "battle", // Default, có thể lấy từ room_settings
            CreatedAt = room.CreatedAt,
            Players = players.ToList()
        };
    }

    public async Task<bool> StartGameAsync(int roomId, int userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null || room.OwnerId != userId || room.Status != "waiting") return false;

        var playerCount = await _roomRepository.GetPlayerCountAsync(roomId);
        if (playerCount < 2) return false; // Cần ít nhất 2 người chơi

        await _roomRepository.UpdateStatusAsync(roomId, "active");
        return true;
    }
    
    // Triển khai các phương thức broadcast từ interface IJoinRoomService
    public async Task BroadcastRoomPlayersUpdateAsync(string roomCode)
    {
        await _broadcastService.BroadcastRoomPlayersUpdateAsync(roomCode);
    }
    
    public async Task BroadcastPlayerJoinedAsync(string roomCode, int newPlayerId)
    {
        await _broadcastService.BroadcastPlayerJoinedAsync(roomCode, newPlayerId);
    }
}