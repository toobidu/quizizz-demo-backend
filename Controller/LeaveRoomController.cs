using ConsoleApp1.Config;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class LeaveRoomController
{
    private readonly IRoomManagementService _roomManagementService;
    private readonly IAuthorizationService _authorizationService;

    public LeaveRoomController(IRoomManagementService roomManagementService, IAuthorizationService authorizationService)
    {
        _roomManagementService = roomManagementService;
        _authorizationService = authorizationService;
    }

    public async Task<ApiResponse<bool>> LeaveRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.leave"))
            return ApiResponse<bool>.Fail("Không có quyền rời phòng");

        var result = await _roomManagementService.LeaveRoomAsync(userId, roomId);
        return result 
            ? ApiResponse<bool>.Success(true, "Rời phòng thành công")
            : ApiResponse<bool>.Fail("Không thể rời phòng");
    }
}