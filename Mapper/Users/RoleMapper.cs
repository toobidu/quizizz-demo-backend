using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.DTO.Users;

namespace ConsoleApp1.Mapper;

public static class RoleMapper
{
    public static RoleDTO ToDTO(Role role)
    {
        return new RoleDTO(
            id: role.Id,
            roleName: role.RoleName,
            description: role.Description
        );
    }

    public static Role ToEntity(RoleDTO roleDto)
    {
        return new Role(
            id: roleDto.Id,
            roleName: roleDto.RoleName,
            description: roleDto.Description
        );
    }
}