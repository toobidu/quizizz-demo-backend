using ConsoleApp1.Mapper;
using ConsoleApp1.Mapper.Users;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Service.Implement;
public class RolePermissionServiceImplement : IRolePermissionService
{
    private readonly IRolePermissionRepository _rolePermissionRepo;
    private readonly IPermissionRepository _permissionRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IUserRoleRepository _userRoleRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRedisService _redisService;
    public RolePermissionServiceImplement(
        IRolePermissionRepository rolePermissionRepo,
        IPermissionRepository permissionRepo,
        IRoleRepository roleRepo,
        IUserRoleRepository userRoleRepo,
        IUserRepository userRepo,
        IRedisService redisService)
    {
        _rolePermissionRepo = rolePermissionRepo;
        _permissionRepo = permissionRepo;
        _roleRepo = roleRepo;
        _userRoleRepo = userRoleRepo;
        _userRepo = userRepo;
        _redisService = redisService;
    }
    public async Task AddPermissionToRoleAsync(int roleId, int permissionId)
    {
        var exists = await _rolePermissionRepo.GetByRoleIdAndPermissionIdAsync(roleId, permissionId);
        if (exists != null) return;
        var permission = await _permissionRepo.GetByIdAsync(permissionId);
        var role = await _roleRepo.GetByIdAsync(roleId);
        if (permission == null || role == null)
            throw new Exception("Permission ho?c Role không t?n t?i");
        var rolePermission = RolePermissionMapper.ToEntity(new RolePermissionDTO(roleId, permissionId));
        await _rolePermissionRepo.AddAsync(rolePermission);
        await UpdateRedisPermissionsForRole(roleId);
    }
    public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId)
    {
        await _rolePermissionRepo.DeleteByRoleIdAndPermissionIdAsync(roleId, permissionId);
        await UpdateRedisPermissionsForRole(roleId);
    }
    public async Task AddPermissionsToRoleAsync(int roleId, List<int> permissionIds)
    {
        foreach (var pid in permissionIds)
        {
            await AddPermissionToRoleAsync(roleId, pid);
        }
    }
    public async Task RemovePermissionsFromRoleAsync(int roleId, List<int> permissionIds)
    {
        foreach (var pid in permissionIds)
        {
            await RemovePermissionFromRoleAsync(roleId, pid);
        }
    }
    private async Task UpdateRedisPermissionsForRole(int roleId)
    {
        var rolePermissions = await _rolePermissionRepo.GetByRoleIdAsync(roleId);
        var permissionIds = rolePermissions.Select(rp => rp.PermissionId).Distinct().ToList();
        var permissionEntities = (await _permissionRepo.GetByIdsAsync(permissionIds)).ToList();
        var permissionNames = permissionEntities
            .Where(p => !string.IsNullOrWhiteSpace(p.PermissionName))
            .Select(p => p.PermissionName)
            .ToList();
        var userRoles = await _userRoleRepo.GetByRoleIdAsync(roleId);
        var userIds = userRoles.Select(ur => ur.UserId).Distinct();
        foreach (var userId in userIds)
        {
            try
            {
                if (permissionNames.Count > 0)
                {
                    await _redisService.SetPermissionsAsync(userId, permissionNames);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
