using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class PermissionMapper
{
    public static PermissionDTO ToDTO(Permission permission)
    {
        return new PermissionDTO(
            id: permission.Id,
            permissionName: permission.PermissionName
        );
    }

    public static Permission ToEntity(PermissionDTO permissionDto)
    {
        return new Permission(
            id: permissionDto.Id,
            permissionName: permissionDto.PermissionName
        );
    }
}