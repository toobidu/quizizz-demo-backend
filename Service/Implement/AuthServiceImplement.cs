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
                Console.WriteLine("Invalid registration fields");
                return false;
            }
            if (request.Password != request.ConfirmPassword)
            {
                Console.WriteLine("Password and confirm password do not match");
                return false;
            }
            var userExists = await _userRepo.ExistsByUsernameAsync(request.Username);
            Console.WriteLine($"User exists check: {userExists}");
            if (userExists)
            {
                Console.WriteLine($"Username {request.Username} already exists");
                return false;
            }

            const string defaultTypeAccount = "PLAYER";
            var role = await _roleRepo.GetByRoleNameAsync(defaultTypeAccount);
            Console.WriteLine($"Role check result: {role?.RoleName ?? "null"}");
            if (role == null)
            {
                Console.WriteLine($"Role {defaultTypeAccount} not found");
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

            var userId = await _userRepo.AddAsync(userEntity);
            Console.WriteLine($"New user created with ID: {userId}");

            var userRole = UserRoleMapper.ToEntity(new UserRoleDTO(userId, role.Id));
            await _userRoleRepo.AddAsync(userRole);
            Console.WriteLine($"User role mapping created for user {userId} and role {role.Id}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during registration: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
    
    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
{
    if (!request.ValidField())
    {
        Console.WriteLine("Invalid login fields");
        return null;
    }

    var user = await _userRepo.GetByUsernameAsync(request.Username);
    if (user == null)
    {
        Console.WriteLine($"User not found: {request.Username}");
        return null;
    }

    bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
    Console.WriteLine($"Password verification result: {passwordValid}");

    if (!passwordValid)
        return null;

    try
    {
        string accessToken = _jwt.GenerateAccessToken(user.Id, user.Username, user.TypeAccount);
        if (string.IsNullOrEmpty(accessToken))
        {
            Console.WriteLine("Failed to generate access token");
            return null;
        }

        string refreshToken = _jwt.GenerateRefreshToken();
        if (string.IsNullOrEmpty(refreshToken))
        {
            Console.WriteLine("Failed to generate refresh token");
            return null;
        }

        //Lấy danh sách quyền từ userId 
        var permissionNames = (await _permissionRepo.GetPermissionsByUserIdAsync(user.Id)).Distinct().ToList();

        if (permissionNames.Any())
        {
            await _redisService.SetPermissionsAsync(user.Id, permissionNames);
            Console.WriteLine($"Permissions saved for user {user.Id}: {string.Join(", ", permissionNames)}");
        }
        else
        {
            Console.WriteLine($"No permissions found for user {user.Id}");
        }

        // Lưu refresh token
        await _redisService.SetRefreshTokenAsync(user.Id, refreshToken,
            TimeSpan.FromDays(_security.RefreshTokenExpirationDays));
        Console.WriteLine($"Refresh token saved for user {user.Id}");

        return new LoginResponse(accessToken, refreshToken);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error during login: {ex.Message}");
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
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                Console.WriteLine($"User with ID {userId} not found");
                return false;
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.Password);
            if (!passwordValid)
            {
                Console.WriteLine("Old password is incorrect");
                return false;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Password = hash;
            
            await _userRepo.UpdateAsync(user);
            Console.WriteLine($"Password updated for user {userId}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing password: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        try
        {
            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null)
            {
                Console.WriteLine($"User with email {email} not found");
                return false;
            }

            // Tạo mật khẩu tạm thời ngẫu nhiên
            string tempPassword = Guid.NewGuid().ToString("N").Substring(0, 8);
            var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            
            user.Password = hash;
            await _userRepo.UpdateAsync(user);
            
            // Trong ứng dụng thực tế, gửi mật khẩu tạm thời qua email
            Console.WriteLine($"Password reset for user with email {email}. Temporary password: {tempPassword}");
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting password: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> SendForgotPasswordOtpAsync(string email)
    {
        try
        {
            Console.WriteLine($"[AUTH_SERVICE] SendForgotPasswordOtpAsync called with email: '{email}'");
            Console.WriteLine($"[AUTH_SERVICE] Email trimmed: '{email?.Trim()}'");
            
            var user = await _userRepo.GetByEmailAsync(email?.Trim() ?? string.Empty);
            Console.WriteLine($"[AUTH_SERVICE] User lookup result: {(user != null ? $"Found user ID {user.Id}" : "User not found")}");
            
            if (user == null)
            {
                Console.WriteLine($"[AUTH_SERVICE] User with email '{email}' not found in database");
                return false;
            }

            // Tạo mã OTP 6 ký tự
            string otpCode = GenerateOtpCode();
            Console.WriteLine($"[AUTH_SERVICE] Generated OTP: {otpCode}");
            
            // Lưu OTP vào Redis với thời gian hết hạn 5 phút
            string otpKey = $"forgot_password_otp:{email?.Trim()}";
            Console.WriteLine($"[AUTH_SERVICE] Redis key: '{otpKey}'");
            
            await _redisService.SetStringAsync(otpKey, otpCode, TimeSpan.FromMinutes(5));
            Console.WriteLine($"[AUTH_SERVICE] OTP saved to Redis successfully");
            
            // Gửi OTP qua email
            bool emailSent = await _emailService.SendOtpEmailAsync(email?.Trim() ?? string.Empty, otpCode);
            Console.WriteLine($"[AUTH_SERVICE] Email sent result: {emailSent}");
            
            if (!emailSent)
            {
                Console.WriteLine($"[AUTH_SERVICE] Failed to send email, but OTP is available in console: {otpCode}");
            }
            
            Console.WriteLine($"[AUTH_SERVICE] OTP for {email}: {otpCode}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AUTH_SERVICE] Error sending OTP: {ex.Message}");
            Console.WriteLine($"[AUTH_SERVICE] StackTrace: {ex.StackTrace}");
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
                Console.WriteLine($"OTP not found or expired for email: {email}");
                return false;
            }

            if (storedOtp != otpCode)
            {
                Console.WriteLine($"Invalid OTP for email: {email}");
                return false;
            }

            Console.WriteLine($"OTP verified successfully for email: {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying OTP: {ex.Message}");
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

            var user = await _userRepo.GetByEmailAsync(email);
            if (user == null)
            {
                Console.WriteLine($"User with email {email} not found");
                return false;
            }

            // Kiểm tra mật khẩu mới không được giống mật khẩu cũ
            bool isSamePassword = BCrypt.Net.BCrypt.Verify(newPassword, user.Password);
            if (isSamePassword)
            {
                Console.WriteLine("New password cannot be the same as old password");
                return false;
            }

            // Cập nhật mật khẩu mới
            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.Password = hash;
            await _userRepo.UpdateAsync(user);

            // Xóa OTP sau khi sử dụng thành công
            string otpKey = $"forgot_password_otp:{email}";
            await _redisService.DeleteAsync(otpKey);

            Console.WriteLine($"Password reset successfully for email: {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error resetting password with OTP: {ex.Message}");
            return false;
        }
    }

    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}