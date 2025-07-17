using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using System.Text.Json;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace ConsoleApp1.Service.Implement;

public class BroadcastServiceImplement : IBroadcastService
{
    private readonly ISocketService _socketService;
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IUserRepository _userRepository;
    private IJoinRoomService? _joinRoomService;

    public BroadcastServiceImplement(
        ISocketService socketService,
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IUserRepository userRepository,
        IJoinRoomService? joinRoomService)
    {
        _socketService = socketService;
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _joinRoomService = joinRoomService;
    }

    public void SetJoinRoomService(IJoinRoomService joinRoomService)
    {
        _joinRoomService = joinRoomService;
    }

    public async Task BroadcastRoomPlayersUpdateAsync(string roomCode)
    {
        try
        {
            var room = await _roomRepository.GetByCodeAsync(roomCode);
            if (room == null) return;

            // Lấy danh sách players trong phòng
            var roomPlayers = await _roomPlayerRepository.GetByRoomIdAsync(room.Id);
            var playerDetails = new List<object>();

            foreach (var rp in roomPlayers)
            {
                var user = await _userRepository.GetByIdAsync(rp.UserId);
                if (user != null)
                {
                    playerDetails.Add(new
                    {
                        userId = user.Id,
                        username = user.Username,
                        isHost = room.OwnerId == user.Id,
                        joinTime = rp.CreatedAt,
                        score = rp.Score
                    });
                }
            }

            var message = new
            {
                Type = "ROOM_PLAYERS_UPDATED",
                RoomCode = roomCode,
                Data = new
                {
                    roomCode = roomCode,
                    players = playerDetails,
                    totalPlayers = playerDetails.Count,
                    maxPlayers = room.MaxPlayers,
                    status = room.Status,
                    host = playerDetails.FirstOrDefault(p => (bool)p.GetType().GetProperty("isHost")!.GetValue(p)!)
                },
                Timestamp = DateTime.UtcNow
            };

            // Broadcast qua WebSocket
            await _socketService.UpdateRoomPlayersAsync(roomCode);
            
            Console.WriteLine($"[BROADCAST] Room players updated for {roomCode}: {playerDetails.Count} players");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BROADCAST] Error broadcasting room players update: {ex.Message}");
        }
    }

    public async Task BroadcastPlayerJoinedAsync(string roomCode, int newPlayerId)
    {
        try
        {
            var room = await _roomRepository.GetByCodeAsync(roomCode);
            if (room == null) return;

            var user = await _userRepository.GetByIdAsync(newPlayerId);
            if (user == null) return;

            var roomPlayer = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(newPlayerId, room.Id);
            if (roomPlayer == null) return;

            Console.WriteLine($"[BROADCAST] Player joined event for {roomCode}: {user.Username} (ID: {user.Id})");
            Console.WriteLine($"[BROADCAST] Player data: UserId={user.Id}, Username={user.Username}, Score={roomPlayer.Score}");
            
            // Broadcast sự kiện player-joined trực tiếp qua WebSocket
            await _socketService.BroadcastPlayerJoinedEventAsync(roomCode, user.Id, user.Username);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BROADCAST] Error broadcasting player joined: {ex.Message}");
        }
    }

    public async Task BroadcastRoomCreatedAsync(RoomDTO room)
    {
        try
        {
            var message = new
            {
                Type = "ROOM_CREATED",
                RoomCode = room.Code,
                Data = room,
                Timestamp = DateTime.UtcNow
            };

            // Broadcast danh sách phòng mới cho lobby
            await BroadcastRoomsListUpdateAsync();
            
            Console.WriteLine($"[BROADCAST] Room created: {room.Code}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BROADCAST] Error broadcasting room created: {ex.Message}");
        }
    }

    public async Task BroadcastRoomDeletedAsync(string roomCode)
    {
        try
        {
            var message = new
            {
                Type = "ROOM_DELETED",
                RoomCode = roomCode,
                Data = new { roomCode },
                Timestamp = DateTime.UtcNow
            };

            // Broadcast danh sách phòng mới cho lobby
            await BroadcastRoomsListUpdateAsync();
            
            Console.WriteLine($"[BROADCAST] Room deleted: {roomCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BROADCAST] Error broadcasting room deleted: {ex.Message}");
        }
    }

    public async Task BroadcastRoomsListUpdateAsync()
    {
        try
        {
            if (_joinRoomService == null)
            {
                Console.WriteLine("[BROADCAST] JoinRoomService not initialized yet, skipping rooms list update");
                return;
            }

            // Lấy danh sách tất cả phòng
            var rooms = await _joinRoomService.GetAllRoomsAsync();
            
            var message = new
            {
                Type = "ROOMS_LIST_UPDATED",
                RoomCode = (string?)null,
                Data = new
                {
                    rooms = rooms,
                    totalRooms = rooms.Count()
                },
                Timestamp = DateTime.UtcNow
            };

            // TODO: Broadcast cho tất cả client đang ở lobby
            // await _socketService.BroadcastToAllAsync(message);
            
            Console.WriteLine($"[BROADCAST] Rooms list updated: {rooms.Count()} rooms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[BROADCAST] Error broadcasting rooms list: {ex.Message}");
        }
    }
}