using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class JoinRoomController
{
    private readonly IJoinRoomService _joinRoomService;
    private readonly IAuthorizationService _authorizationService;

    public JoinRoomController(IJoinRoomService joinRoomService, IAuthorizationService authorizationService)
    {
        _joinRoomService = joinRoomService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Xem danh sách phòng public
    /// Quyền: room.join
    /// </summary>
    public async Task<ApiResponse<IEnumerable<RoomSummaryDTO>>> GetPublicRoomsAsync(int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.join"))
            return ApiResponse<IEnumerable<RoomSummaryDTO>>.Fail("Không có quyền xem danh sách phòng");

        var rooms = await _joinRoomService.GetPublicRoomsAsync();
        return ApiResponse<IEnumerable<RoomSummaryDTO>>.Success(rooms);
    }

    /// <summary>
    /// Tham gia phòng public
    /// Quyền: room.join
    /// </summary>
    public async Task<ApiResponse<RoomDTO>> JoinPublicRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.join"))
            return ApiResponse<RoomDTO>.Fail("Không có quyền tham gia phòng");

        var room = await _joinRoomService.JoinPublicRoomAsync(roomId, userId);
        return room != null 
            ? ApiResponse<RoomDTO>.Success(room, "Tham gia phòng thành công")
            : ApiResponse<RoomDTO>.Fail("Không thể tham gia phòng");
    }

    /// <summary>
    /// Tham gia phòng private bằng room code
    /// Quyền: room.join
    /// </summary>
    public async Task<ApiResponse<RoomDTO>> JoinPrivateRoomAsync(string roomCode, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.join"))
            return ApiResponse<RoomDTO>.Fail("Không có quyền tham gia phòng");

        if (string.IsNullOrWhiteSpace(roomCode) || roomCode.Length != 6)
            return ApiResponse<RoomDTO>.Fail("Mã phòng không hợp lệ");

        var room = await _joinRoomService.JoinPrivateRoomAsync(roomCode, userId);
        return room != null 
            ? ApiResponse<RoomDTO>.Success(room, "Tham gia phòng thành công")
            : ApiResponse<RoomDTO>.Fail("Không thể tham gia phòng");
    }

    /// <summary>
    /// Rời khỏi phòng
    /// Quyền: room.leave
    /// </summary>
    public async Task<ApiResponse<bool>> LeaveRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.leave"))
            return ApiResponse<bool>.Fail("Không có quyền rời phòng");

        var result = await _joinRoomService.LeaveRoomAsync(roomId, userId);
        return result 
            ? ApiResponse<bool>.Success(true, "Rời phòng thành công")
            : ApiResponse<bool>.Fail("Không thể rời phòng");
    }

    /// <summary>
    /// Xem thông tin phòng bằng room code
    /// Quyền: room.join
    /// </summary>
    public async Task<ApiResponse<RoomDTO>> GetRoomByCodeAsync(string roomCode, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.join"))
            return ApiResponse<RoomDTO>.Fail("Không có quyền xem thông tin phòng");

        var room = await _joinRoomService.GetRoomByCodeAsync(roomCode);
        return room != null 
            ? ApiResponse<RoomDTO>.Success(room)
            : ApiResponse<RoomDTO>.Fail("Không tìm thấy phòng");
    }

    /// <summary>
    /// Xem danh sách người chơi trong phòng
    /// Quyền: room.join
    /// </summary>
    public async Task<ApiResponse<IEnumerable<PlayerInRoomDTO>>> GetPlayersInRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.join"))
            return ApiResponse<IEnumerable<PlayerInRoomDTO>>.Fail("Không có quyền xem danh sách người chơi");

        var players = await _joinRoomService.GetPlayersInRoomAsync(roomId);
        return ApiResponse<IEnumerable<PlayerInRoomDTO>>.Success(players);
    }
}