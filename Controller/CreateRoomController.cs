using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class CreateRoomController
{
    private readonly ICreateRoomService _createRoomService;
    private readonly IAuthorizationService _authorizationService;

    public CreateRoomController(ICreateRoomService createRoomService, IAuthorizationService authorizationService)
    {
        _createRoomService = createRoomService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Tạo phòng chơi mới
    /// Quyền: room.create
    /// </summary>
    public async Task<ApiResponse<RoomDTO>> CreateRoomAsync(CreateRoomRequest request, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.create"))
            return ApiResponse<RoomDTO>.Fail("Không có quyền tạo phòng");

        if (!request.ValidField())
            return ApiResponse<RoomDTO>.Fail("Thông tin phòng không hợp lệ");

        var room = await _createRoomService.CreateRoomAsync(request);
        return ApiResponse<RoomDTO>.Success(room, "Tạo phòng thành công");
    }

    /// <summary>
    /// Cập nhật cài đặt phòng
    /// Quyền: room.settings.update (chỉ host của phòng)
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateRoomSettingsAsync(int roomId, RoomSetting settings, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.settings.update"))
            return ApiResponse<bool>.Fail("Không có quyền cập nhật cài đặt phòng");

        var result = await _createRoomService.UpdateRoomSettingsAsync(roomId, settings);
        return result 
            ? ApiResponse<bool>.Success(true, "Cập nhật cài đặt thành công")
            : ApiResponse<bool>.Fail("Cập nhật cài đặt thất bại");
    }

    /// <summary>
    /// Đuổi người chơi khỏi phòng
    /// Quyền: room.kick_player (chỉ host của phòng)
    /// </summary>
    public async Task<ApiResponse<bool>> KickPlayerAsync(int roomId, int playerId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.kick_player"))
            return ApiResponse<bool>.Fail("Không có quyền đuổi người chơi");

        var result = await _createRoomService.KickPlayerAsync(roomId, playerId);
        return result 
            ? ApiResponse<bool>.Success(true, "Đuổi người chơi thành công")
            : ApiResponse<bool>.Fail("Đuổi người chơi thất bại");
    }

    /// <summary>
    /// Cập nhật trạng thái phòng
    /// Quyền: room.start_game (chỉ host của phòng)
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateRoomStatusAsync(int roomId, string status, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.start_game"))
            return ApiResponse<bool>.Fail("Không có quyền thay đổi trạng thái phòng");

        var result = await _createRoomService.UpdateRoomStatusAsync(roomId, status);
        return result 
            ? ApiResponse<bool>.Success(true, "Cập nhật trạng thái thành công")
            : ApiResponse<bool>.Fail("Cập nhật trạng thái thất bại");
    }

    /// <summary>
    /// Xóa phòng của mình
    /// Quyền: room.delete_own (chỉ host của phòng)
    /// </summary>
    public async Task<ApiResponse<bool>> DeleteRoomAsync(int roomId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.delete_own"))
            return ApiResponse<bool>.Fail("Không có quyền xóa phòng");

        var result = await _createRoomService.DeleteRoomAsync(roomId);
        return result 
            ? ApiResponse<bool>.Success(true, "Xóa phòng thành công")
            : ApiResponse<bool>.Fail("Xóa phòng thất bại");
    }

    /// <summary>
    /// Chuyển quyền host cho người khác
    /// Quyền: room.settings.update (chỉ host hiện tại)
    /// </summary>
    public async Task<ApiResponse<bool>> TransferOwnershipAsync(int roomId, int newOwnerId, int userId)
    {
        if (!await _authorizationService.HasPermissionAsync(userId, "room.settings.update"))
            return ApiResponse<bool>.Fail("Không có quyền chuyển quyền host");

        var result = await _createRoomService.TransferOwnershipAsync(roomId, newOwnerId);
        return result 
            ? ApiResponse<bool>.Success(true, "Chuyển quyền host thành công")
            : ApiResponse<bool>.Fail("Chuyển quyền host thất bại");
    }
}