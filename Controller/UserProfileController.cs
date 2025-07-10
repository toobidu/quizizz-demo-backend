using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class UserProfileController
{
    private readonly IUserProfileService _userProfileService;
    private readonly IAuthorizationService _authorizationService;

    public UserProfileController(IUserProfileService userProfileService, IAuthorizationService authorizationService)
    {
        _userProfileService = userProfileService;
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Xem hồ sơ của chính mình
    /// Quyền: Tự động có (user tự xem profile mình)
    /// </summary>
    public async Task<ApiResponse<UserProfileDTO>> GetMyProfileAsync(int userId)
    {
        var profile = await _userProfileService.GetUserProfileAsync(userId);
        return profile != null 
            ? ApiResponse<UserProfileDTO>.Success(profile)
            : ApiResponse<UserProfileDTO>.Fail("Không tìm thấy hồ sơ");
    }

    /// <summary>
    /// Tìm kiếm và xem hồ sơ người khác
    /// Quyền: Tự động có (tất cả user có thể xem profile công khai)
    /// </summary>
    public async Task<ApiResponse<UserProfileDTO>> SearchUserAsync(string username, int userId)
    {
        if (string.IsNullOrWhiteSpace(username))
            return ApiResponse<UserProfileDTO>.Fail("Tên người dùng không được để trống");

        var profile = await _userProfileService.SearchUserByUsernameAsync(username);
        return profile != null 
            ? ApiResponse<UserProfileDTO>.Success(profile)
            : ApiResponse<UserProfileDTO>.Fail("Không tìm thấy người dùng");
    }

    /// <summary>
    /// Thay đổi mật khẩu của chính mình
    /// Quyền: Tự động có (user tự đổi password mình)
    /// </summary>
    public async Task<ApiResponse<bool>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (!request.IsValid())
            return ApiResponse<bool>.Fail("Yêu cầu đổi mật khẩu không hợp lệ");

        var result = await _userProfileService.ChangePasswordAsync(userId, request);
        return result 
            ? ApiResponse<bool>.Success(true, "Đổi mật khẩu thành công")
            : ApiResponse<bool>.Fail("Đổi mật khẩu thất bại");
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân của chính mình
    /// Quyền: Tự động có (user tự update profile mình)
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var result = await _userProfileService.UpdateProfileAsync(userId, request);
        return result 
            ? ApiResponse<bool>.Success(true, "Cập nhật hồ sơ thành công")
            : ApiResponse<bool>.Fail("Cập nhật hồ sơ thất bại");
    }
}