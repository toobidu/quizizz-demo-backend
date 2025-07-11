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
        Console.WriteLine($"[ROOM_MANAGEMENT] User {userId} leaving room {roomId}");
        
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            Console.WriteLine($"[ROOM_MANAGEMENT] Room {roomId} not found");
            return false;
        }

        // Xóa user khỏi room_players
        var removed = await _roomPlayerRepository.DeleteByUserIdAndRoomIdAsync(userId, roomId);
        if (!removed)
        {
            Console.WriteLine($"[ROOM_MANAGEMENT] User {userId} was not in room {roomId}");
            return false;
        }

        // Kiểm tra nếu user là host
        if (room.OwnerId == userId)
        {
            Console.WriteLine($"[ROOM_MANAGEMENT] Host {userId} is leaving room {roomId}");
            await HandleHostLeaving(roomId);
        }

        Console.WriteLine($"[ROOM_MANAGEMENT] User {userId} successfully left room {roomId}");
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