using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Mapper.Users;

public static class PermissionMapper
{
    public static PermissionDTO ToDTO(Permission permission)
    {
        return new PermissionDTO(
            id: permission.Id,
            permissionName: permission.PermissionName,
            description: permission.Description
        );
    }

    public static Permission ToEntity(PermissionDTO permissionDto)
    {
        return new Permission(
            id: permissionDto.Id,
            permissionName: permissionDto.PermissionName,
            description: permissionDto.Description,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
    }
}