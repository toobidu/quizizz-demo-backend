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
/* <<<<<<<<<<<<<<  ✨ Windsurf Command ⭐ >>>>>>>>>>>>>>>> */
    /// <summary>
    /// Rời khỏi phòng
    /// Quyền: room.leave
    /// </summary>
    /// <param name="roomId">ID của phòng</param>
    /// <param name="userId">ID của người chơi</param>
    /// <returns>Trạng thái rời phòng</returns>
/* <<<<<<<<<<  79ed9321-1a85-4ca8-b53c-51998af013bf  >>>>>>>>>>> */
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
