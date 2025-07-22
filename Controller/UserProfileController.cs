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
        try
        {
            var profile = await _userProfileService.GetUserProfileAsync(userId);
            if (profile != null)
            {
                // Đánh dấu đây là profile của chính mình
                profile.IsOwnProfile = true;
                return ApiResponse<UserProfileDTO>.Success(profile);
            }
            else
            {
                return ApiResponse<UserProfileDTO>.Fail("Không tìm thấy hồ sơ");
            }
        }
        catch (Exception ex)
        {
            return ApiResponse<UserProfileDTO>.Fail($"Lỗi khi lấy hồ sơ: {ex.Message}");
        }
    }
    /// <summary>
    /// Tìm kiếm và xem hồ sơ người khác
    /// Quyền: Tự động có (tất cả user có thể xem profile công khai)
    /// </summary>
    public async Task<ApiResponse<UserProfileDTO>> SearchUserAsync(string username, int currentUserId)
    {
        if (string.IsNullOrWhiteSpace(username))
            return ApiResponse<UserProfileDTO>.Fail("Tên người dùng không được để trống");
        var profile = await _userProfileService.SearchUserByUsernameAsync(username);
        if (profile == null)
            return ApiResponse<UserProfileDTO>.Fail("Không tìm thấy người dùng");
        // Đánh dấu xem có phải profile của chính mình không
        profile.IsOwnProfile = profile.Id == currentUserId;
        return ApiResponse<UserProfileDTO>.Success(profile);
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
    /// Thay đổi mật khẩu qua profile ID (với kiểm tra quyền)
    /// Quyền: Chỉ được đổi password của chính mình
    /// </summary>
    public async Task<ApiResponse<bool>> ChangePasswordByIdAsync(int profileId, int currentUserId, ChangePasswordRequest request)
    {
        // Kiểm tra quyền: chỉ được đổi password của chính mình
        if (profileId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền thay đổi mật khẩu của người khác");
        }
        if (!request.IsValid())
            return ApiResponse<bool>.Fail("Yêu cầu đổi mật khẩu không hợp lệ");
        var result = await _userProfileService.ChangePasswordAsync(profileId, request);
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
    /// <summary>
    /// Cập nhật thông tin cá nhân qua profile ID (với kiểm tra quyền)
    /// Quyền: Chỉ được update profile của chính mình
    /// </summary>
    public async Task<ApiResponse<bool>> UpdateProfileByIdAsync(int profileId, int currentUserId, UpdateProfileRequest request)
    {
        // Kiểm tra quyền: chỉ được update profile của chính mình
        if (profileId != currentUserId)
        {
            return ApiResponse<bool>.Fail("Bạn không có quyền chỉnh sửa hồ sơ của người khác");
        }
        var result = await _userProfileService.UpdateProfileAsync(profileId, request);
        return result 
            ? ApiResponse<bool>.Success(true, "Cập nhật hồ sơ thành công")
            : ApiResponse<bool>.Fail("Cập nhật hồ sơ thất bại");
    }
}
