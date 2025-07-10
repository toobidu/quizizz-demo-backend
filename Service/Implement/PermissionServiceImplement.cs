using ConsoleApp1.Mapper;
using ConsoleApp1.Mapper.Users;
using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Service;

public class PermissionServiceImplement : IPermissionService
{
    private readonly IPermissionRepository _permissionRepo;

    public PermissionServiceImplement(IPermissionRepository permissionRepo)
    {
        _permissionRepo = permissionRepo;
    }

    public async Task<List<PermissionDTO>> GetAllPermissionsAsync()
    {
        var permissions = await _permissionRepo.GetAllAsync();
        return permissions.Select(PermissionMapper.ToDTO).ToList();
    }

    public async Task<PermissionDTO?> GetPermissionByIdAsync(int id)
    {
        var permission = await _permissionRepo.GetByIdAsync(id);
        return permission != null ? PermissionMapper.ToDTO(permission) : null;
    }

    public async Task<bool> PermissionNameExistsAsync(string permissionName)
    {
        return await _permissionRepo.ExistsByPermissionNameAsync(permissionName);
    }

    public async Task<bool> AddPermissionAsync(PermissionDTO permissionDto)
    {
        var exists = await _permissionRepo.ExistsByPermissionNameAsync(permissionDto.PermissionName);
        if (exists) return false;

        var permission = PermissionMapper.ToEntity(permissionDto);
        await _permissionRepo.AddAsync(permission);
        return true;
    }

    public async Task<bool> UpdatePermissionAsync(PermissionDTO permissionDto)
    {
        var existing = await _permissionRepo.GetByIdAsync(permissionDto.Id);
        if (existing == null) return false;

        var updated = PermissionMapper.ToEntity(permissionDto);
        await _permissionRepo.UpdateAsync(updated);
        return true;
    }

    public async Task<bool> DeletePermissionAsync(int id)
    {
        return await _permissionRepo.DeleteAsync(id);
    }
}