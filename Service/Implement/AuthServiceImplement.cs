using ConsoleApp1.Config;
using ConsoleApp1.Mapper;
using ConsoleApp1.Mapper.Users;
using ConsoleApp1.Model.DTO.Authentication;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Service.Implement;
public class AuthServiceImplement : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly JwtHelper _jwt;
    private readonly SecurityConfig _security;
    public AuthServiceImplement(
        IUserRepository userRepo,
        IPermissionRepository permissionRepo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        IRedisService redisService,
        IEmailService emailService,
        JwtHelper jwt,
        SecurityConfig security)
    {
        _userRepo = userRepo;
        _permissionRepo = permissionRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _redisService = redisService;
        _emailService = emailService;
        _jwt = jwt;
        _security = security;
    }
    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            if (!request.ValidField())
            {
                return false;
            }
            if (request.Password != request.ConfirmPassword)
            {
                return false;
            }
            var existingUser = await _userRepo.GetUserByUsernameAsync(request.Username);
            var userExists = existingUser != null;
            if (userExists)
            {
                return false;
            }
            const string defaultTypeAccount = "PLAYER";
            var role = await _roleRepo.GetByRoleNameAsync(defaultTypeAccount);
            if (role == null)
            {
                return false;
            }
            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var userEntity = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber, 
                Address = request.Address,
                Password = hash,
                TypeAccount = defaultTypeAccount
            };
            userEntity.Password = hash;
            userEntity.TypeAccount = defaultTypeAccount;
            var userId = await _userRepo.CreateUserAsync(userEntity);
            var userRole = UserRoleMapper.ToEntity(new UserRoleDTO(userId, role.Id));
            await _userRoleRepo.AddAsync(userRole);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
{
    if (!request.ValidField())
    {
        return null;
    }
    var user = await _userRepo.GetUserByUsernameAsync(request.Username);
    if (user == null)
    {
        return null;
    }
    bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
    if (!passwordValid)
        return null;
    try
    {
        string accessToken = _jwt.GenerateAccessToken(user.Id, user.Username, user.TypeAccount);
        if (string.IsNullOrEmpty(accessToken))
        {
            return null;
        }
        string refreshToken = _jwt.GenerateRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }
        // Lấy danh sách quyền từ userId 
        var permissionNames = (await _permissionRepo.GetPermissionsByUserIdAsync(user.Id)).Distinct().ToList();
        if (permissionNames.Any())
        {
            await _redisService.SetPermissionsAsync(user.Id, permissionNames);
        }
        else
        {
        }
        // Luu refresh token
        await _redisService.SetRefreshTokenAsync(user.Id, refreshToken,
            TimeSpan.FromDays(_security.RefreshTokenExpirationDays));
        return new LoginResponse(accessToken, refreshToken);
    }
    catch (Exception ex)
    {
        return null;
    }
}
    public async Task<bool> LogoutAsync(string accessToken)
    {
        int? userId = _jwt.GetUserIdFromToken(accessToken);
        if (userId == null)
            return false;
        await _redisService.DeleteRefreshTokenAsync(userId.Value);
        return true;
    }
    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
            {
                return false;
            }
            bool passwordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.Password);
            if (!passwordValid)
            {
                return false;
            }
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Password = hash;
            await _userRepo.UpdateAsync(user);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepo.GetUserByEmailAsync(email);
            if (user == null)
            {
                return false;
            }
            // Tạo mật khẩu tạm thời ngẫu nhiên
            string tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8);
            var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.Password = hash;
            await _userRepo.UpdateUserAsync(user);
            // Trong ứng dụng thực tế, gửi mật khẩu tạm thời qua email
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> SendForgotPasswordOtpAsync(string email)
    {
        try
        {
            var user = await _userRepo.GetUserByEmailAsync(email?.Trim() ?? string.Empty);
            if (user == null)
            {
                return false;
            }
            // Tạo mã OTP 6 ký tự
            string otpCode = GenerateOtpCode();
            // Lưu OTP vào Redis với thời gian hết hạn 5 phút
            string otpKey = $"forgot_password_otp:{email?.Trim()}";
            await _redisService.SetStringAsync(otpKey, otpCode, TimeSpan.FromMinutes(5));
            // Gửi OTP qua email
            bool emailSent = await _emailService.SendOtpEmailAsync(email?.Trim() ?? string.Empty, otpCode);
            if (!emailSent)
            {
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> VerifyOtpAsync(string email, string otpCode)
    {
        try
        {
            string otpKey = $"forgot_password_otp:{email}";
            string? storedOtp = await _redisService.GetStringAsync(otpKey);
            if (string.IsNullOrEmpty(storedOtp))
            {
                return false;
            }
            if (storedOtp != otpCode)
            {
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword)
    {
        try
        {
            // Xác thực OTP trước
            if (!await VerifyOtpAsync(email, otpCode))
            {
                return false;
            }
            var user = await _userRepo.GetUserByEmailAsync(email);
            if (user == null)
            {
                return false;
            }
            // Kiểm tra mật khẩu mới không được giống mật khẩu cũ
            bool isSamePassword = BCrypt.Net.BCrypt.Verify(newPassword, user.Password);
            if (isSamePassword)
            {
                return false;
            }
            // Cập nhật mật khẩu mới
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Password = hash;
            await _userRepo.UpdateUserAsync(user);
            // Xóa OTP sau khi sử dụng thành công
            string otpKey = $"forgot_password_otp:{email}";
            await _redisService.DeleteAsync(otpKey);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}
