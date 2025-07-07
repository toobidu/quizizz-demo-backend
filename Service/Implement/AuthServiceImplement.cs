using ConsoleApp1.Config;
using ConsoleApp1.Mapper;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Security;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRolePermissionRepository _rolePermissionRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IRedisService _redisService;
    private readonly JwtHelper _jwt;
    private readonly SecurityConfig _security;

    public AuthService(
        IUserRepository userRepo,
        IRolePermissionRepository rolePermissionRepo,
        IPermissionRepository permissionRepo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        IRedisService redisService,
        JwtHelper jwt,
        SecurityConfig security)
    {
        _userRepo = userRepo;
        _rolePermissionRepo = rolePermissionRepo;
        _permissionRepo = permissionRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _redisService = redisService;
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

            var userExists = await _userRepo.ExistsByUsernameAsync(request.Username);
            Console.WriteLine($"User exists check: {userExists}");
            if (userExists)
            {
                Console.WriteLine($"Username {request.Username} already exists");
                return false;
            }

            var normalizedType = request.TypeAccount.Trim().ToUpper();
            Console.WriteLine($"Normalized type: {normalizedType}");

            var role = await _roleRepo.GetByRoleNameAsync(normalizedType);
            Console.WriteLine($"Role check result: {role?.RoleName ?? "null"}");
            if (role == null)
            {
                Console.WriteLine($"Role {normalizedType} not found");
                return false;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var userEntity = RegisterRequestMapper.ToEntity(request);
            userEntity.Password = hash;
            userEntity.TypeAccount = normalizedType;

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

        // ✅ Lưu refresh token
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
}