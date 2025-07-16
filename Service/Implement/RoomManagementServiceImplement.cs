using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class RoomManagementServiceImplement : IRoomManagementService
{
    private readonly IRoomRepository _roomRepository;
    private readonly IRoomPlayerRepository _roomPlayerRepository;
    private readonly IRoomSettingsRepository _roomSettingsRepository;
    private readonly IUserRepository _userRepository;

    public RoomManagementServiceImplement(
        IRoomRepository roomRepository,
        IRoomPlayerRepository roomPlayerRepository,
        IRoomSettingsRepository roomSettingsRepository,
        IUserRepository userRepository)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _roomSettingsRepository = roomSettingsRepository;
        _userRepository = userRepository;
    }

    public async Task<bool> LeaveRoomAsync(int userId, int roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return false;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        Console.WriteLine($"[{timestamp}] ❌ PLAYER LEAVING - Room {room.RoomCode} (ID: {roomId}): Player {userId} attempting to leave");
        
        // Xóa player khỏi room_players TRƯỚC
        var deleteResult = await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(userId, roomId);
        if (!deleteResult)
        {
            Console.WriteLine($"[{timestamp}] ⚠️ PLAYER NOT IN ROOM - Player {userId} was not in room {roomId}");
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
        if (room.OwnerId == userId)
        {
            Console.WriteLine($"[{timestamp}] 👑 OWNER LEFT - Room {room.RoomCode}: Checking remaining players");
            
            if (playerCountAfter == 0)
            {
                Console.WriteLine($"[{timestamp}] 🗑️ ROOM DELETED - Room {room.RoomCode} (ID: {roomId}): No players remaining");
                await DeleteRoomIfEmptyAsync(roomId);
            }
            else
            {
                // Chuyển quyền owner cho người chơi đầu tiên còn lại
                var remainingPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
                if (remainingPlayers.Any())
                {
                    var nextOwner = remainingPlayers.OrderBy(p => p.CreatedAt).First();
                    Console.WriteLine($"[{timestamp}] 🔄 OWNERSHIP TRANSFERRED - Room {room.RoomCode}: New owner is Player {nextOwner.UserId}");
                    await TransferHostAsync(roomId, userId, nextOwner.UserId);
                }
            }
        }

        Console.WriteLine($"[{timestamp}] ✅ LEAVE COMPLETED - Player {userId} successfully left room {room.RoomCode}");
        return true;
    }

    public async Task<bool> TransferHostAsync(int roomId, int currentHostId, int newHostId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null || room.OwnerId != currentHostId)
        {
            return false;
        }

        // Kiểm tra newHost có trong phòng không
        var newHostInRoom = await _roomPlayerRepository.GetByUserIdAndRoomIdAsync(newHostId, roomId);
        if (newHostInRoom == null)
        {
            return false;
        }

        // Cập nhật owner
        room.OwnerId = newHostId;
        room.UpdatedAt = DateTime.UtcNow;
        await _roomRepository.UpdateAsync(room);

        // Cập nhật type_account của user mới
        await UpdateUserTypeAccountAsync(newHostId);

        Console.WriteLine($"[ROOM_MANAGEMENT] Host transferred from {currentHostId} to {newHostId} in room {roomId}");
        return true;
    }

    public async Task<bool> DeleteRoomIfEmptyAsync(int roomId)
    {
        var playersInRoom = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        
        if (!playersInRoom.Any())
        {
            Console.WriteLine($"[ROOM_MANAGEMENT] Room {roomId} is empty, deleting...");
            
            // Xóa room settings trước
            await _roomSettingsRepository.DeleteAllSettingsAsync(roomId);
            
            // Xóa room
            await _roomRepository.DeleteAsync(roomId);
            
            Console.WriteLine($"[ROOM_MANAGEMENT] Room {roomId} deleted successfully");
            return true;
        }

        return false;
    }

    private async Task HandleHostLeaving(int roomId)
    {
        // Lấy danh sách players còn lại trong phòng (sắp xếp theo thời gian join)
        var remainingPlayers = await _roomPlayerRepository.GetByRoomIdAsync(roomId);
        
        if (!remainingPlayers.Any())
        {
            // Không còn ai trong phòng, xóa phòng
            Console.WriteLine($"[ROOM_MANAGEMENT] No players left in room {roomId}, deleting room");
            await DeleteRoomIfEmptyAsync(roomId);
            return;
        }

        // Chuyển host cho player join sớm nhất (created_at nhỏ nhất)
        var nextHost = remainingPlayers.OrderBy(p => p.CreatedAt).First();
        
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room != null)
        {
            room.OwnerId = nextHost.UserId;
            room.UpdatedAt = DateTime.UtcNow;
            await _roomRepository.UpdateAsync(room);

            await UpdateUserTypeAccountAsync(nextHost.UserId);
            
            Console.WriteLine($"[ROOM_MANAGEMENT] New host assigned: User {nextHost.UserId} in room {roomId}");
        }
    }

    private async Task UpdateUserTypeAccountAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user != null && user.TypeAccount == "player")
        {
            user.TypeAccount = "host";
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            Console.WriteLine($"[ROOM_MANAGEMENT] User {userId} promoted to host");
        }
    }
}