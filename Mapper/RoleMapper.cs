using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class RoleMapper
{
    public static RoleDTO ToDTO(this Role role) => new RoleDTO
    {
        Id = role.Id,
        RoleName = role.RoleName
    };

    public static Role ToEntity(this RoleDTO dto) => new Role
    {
        Id = dto.Id,
        RoleName = dto.RoleName
    };
}