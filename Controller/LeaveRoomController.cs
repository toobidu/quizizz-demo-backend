using ConsoleApp1.Config;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class LeaveRoomController
{
    private readonly IJoinRoomService _joinRoomService; // Sử dụng JoinRoomService thay vì RoomManagementService
    private readonly IAuthorizationService _authorizationService;

    public LeaveRoomController(IJoinRoomService joinRoomService, IAuthorizationService authorizationService)
    {
        _joinRoomService = joinRoomService;
        _authorizationService = authorizationService;
    }

    public async Task<ApiResponse<bool>> LeaveRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.leave"))
            return ApiResponse<bool>.Fail("Không có quyền rời phòng");

        // Sử dụng JoinRoomService vì nó có logic đầy đủ cho việc rời phòng
        var result = await _joinRoomService.LeaveRoomAsync(roomId, userId);
        return result 
            ? ApiResponse<bool>.Success(true, "Rời phòng thành công")
            : ApiResponse<bool>.Fail("Không thể rời phòng");
    }
}