using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Mapper.Users;

public static class RolePermissionMapper
{
    public static RolePermissionDTO ToDTO(RolePermission rp)
    {
        return new RolePermissionDTO(
            roleId: rp.RoleId,
            permissionId: rp.PermissionId
        );
    }

    public static RolePermission ToEntity(RolePermissionDTO rpDto)
    {
        return new RolePermission(
            roleId: rpDto.RoleId,
            permissionId: rpDto.PermissionId,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
    }
}