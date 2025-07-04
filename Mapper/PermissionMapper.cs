using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class PermissionMapper
{
    public static PermissionDTO ToDTO(this Permission permission) => new PermissionDTO
    {
        Id = permission.Id,
        PermissionName = permission.PermissionName
    };

    public static Permission ToEntity(this PermissionDTO dto) => new Permission
    {
        Id = dto.Id,
        PermissionName = dto.PermissionName
    };
}