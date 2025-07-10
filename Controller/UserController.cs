using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class UserController
{
    private readonly IUserService _userService;
    private readonly IAuthorizationService _authService;
    private readonly JwtHelper _jwt;

    public UserController(
        IUserService userService,
        IAuthorizationService authService,
        JwtHelper jwt)
    {
        _userService = userService;
        _authService = authService;
        _jwt = jwt;
    }

    private async Task<(bool isAuthorized, int userId)> IsAuthorized(string token)
    {
        int? userId = _jwt.GetUserIdFromToken(token);
        if (userId == null) return (false, -1);

        var hasPermission = await _authService.HasPermissionAsync(userId.Value, "ManageUsers");
        return (hasPermission, userId.Value);
    }

    /*
    POST /api/users
    */
    public async Task<string> CreateUserAsync(UserDTO user, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "Bạn không có quyền tạo người dùng.";

        var result = await _userService.CreateUserAsync(user);
        return result ? "Tạo người dùng thành công." : "Tạo người dùng thất bại.";
    }

    /*
    GET /api/users
    */
    public async Task<List<UserDTO>> GetAllUsersAsync(string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return new List<UserDTO>();

        return await _userService.GetAllUsersAsync();
    }

    /*
    GET /api/users/{userId}
    */
    public async Task<UserDTO?> GetUserByIdAsync(int userId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;

        return await _userService.GetUserByIdAsync(userId);
    }

    /*
    PUT /api/users/{userId}
    */
    public async Task<string> UpdateUserAsync(int userId, UserDTO updatedUser, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "Bạn không có quyền cập nhật người dùng.";

        var result = await _userService.UpdateUserAsync(userId, updatedUser);
        return result ? "Cập nhật thành công." : "Cập nhật thất bại.";
    }

    /*
    DELETE /api/users/{userId}
    */
    public async Task<string> DeleteUserAsync(int userId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "Bạn không có quyền xóa người dùng.";

        var result = await _userService.DeleteUserAsync(userId);
        return result ? "Xóa thành công." : "Xóa thất bại.";
    }

    /*
    PUT /api/users/{userId}/type-account
    */
    public async Task<string> UpdateUserTypeAccountAsync(int userId, string newTypeAccount, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return "Bạn không có quyền cập nhật loại tài khoản.";

        var result = await _userService.UpdateUserTypeAccountAsync(userId, newTypeAccount);
        return result ? "Cập nhật loại tài khoản thành công." : "Cập nhật loại tài khoản thất bại.";
    }

    /*
    GET /api/users/{userId}/type-account
    */
    public async Task<string?> GetTypeAccountAsync(int userId, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;

        return await _userService.GetTypeAccountAsync(userId);
    }

    /*
    GET /api/users/map-role?typeAccount={typeAccount}
    */
    public async Task<int?> MapTypeAccountToRoleIdAsync(string typeAccount, string accessToken)
    {
        var (authorized, _) = await IsAuthorized(accessToken);
        if (!authorized) return null;

        return await _userService.MapTypeAccountToRoleIdAsync(typeAccount);
    }
}
