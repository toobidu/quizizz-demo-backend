using ConsoleApp1.Mapper;
using ConsoleApp1.Mapper.Users;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service.Implement;

public class RoleServiceImplement : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    
    public RoleServiceImplement(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserRoleRepository userRoleRepository )
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _userRoleRepository = userRoleRepository;
        _rolePermissionRepository = rolePermissionRepository;
    }
    
    public async Task<RoleDTO> GetRoleByIdAsync(int id)
    {
        var role = await _roleRepository.GetByIdAsync(id);
        return role != null ? RoleMapper.ToDTO(role) : null!;
    }

    public async Task<List<RoleDTO>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return roles.Select(RoleMapper.ToDTO).ToList();
    }

    public async Task<RoleDTO> CreateRoleAsync(RoleDTO role)
    {
        var entity = RoleMapper.ToEntity(role);
        var roleId = await _roleRepository.AddAsync(entity);
        entity.Id = roleId;
        return RoleMapper.ToDTO(entity);
    }

    public async Task<RoleDTO> UpdateRoleAsync(RoleDTO role)
    {
        var entity = RoleMapper.ToEntity(role);
        await _roleRepository.UpdateAsync(entity);
        return RoleMapper.ToDTO(entity);
    }

    public async Task DeleteRoleAsync(int id)
    {
        await _roleRepository.DeleteAsync(id);
    }

    public async Task<List<RoleDTO>> GetRolesByUserIdAsync(int userId)
    {
        var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
        var roles = userRoles.Select(ur => _roleRepository.GetByIdAsync(ur.RoleId).Result).Where(r => r != null);
        return roles.Select(RoleMapper.ToDTO).ToList();
    }

    public async Task<bool> RoleNameExistsAsync(string roleName)
    {
        return await _roleRepository.ExistsByRoleNameAsync(roleName);
    }

    public async Task<List<RoleDTO>> GetRolesByPermissionIdAsync(int permissionId)
    {
        var rolePermissions = await _rolePermissionRepository.GetByPermissionIdAsync(permissionId);
        var roles = rolePermissions.Select(rp => _roleRepository.GetByIdAsync(rp.RoleId).Result).Where(r => r != null);
        return roles.Select(RoleMapper.ToDTO).ToList();
    }

    public async Task<List<RoleDTO>> GetRolesByUserIdAndPermissionNamesAsync(int userId, List<string> permissionNames)
    {
        var userRoles = await _userRoleRepository.GetByUserIdAsync(userId);
        var roles = new List<RoleDTO>();

        foreach (var userRole in userRoles)
        {
            var rolePermissions = await _rolePermissionRepository.GetByRoleIdAsync(userRole.RoleId);
            var permissions = rolePermissions.Select(rp => _permissionRepository.GetByIdAsync(rp.PermissionId).Result)
                                              .Where(p => p != null && permissionNames.Contains(p.PermissionName));

            if (permissions.Any())
            {
                var role = await _roleRepository.GetByIdAsync(userRole.RoleId);
                if (role != null)
                {
                    roles.Add(RoleMapper.ToDTO(role));
                }
            }
        }

        return roles;
    }
}