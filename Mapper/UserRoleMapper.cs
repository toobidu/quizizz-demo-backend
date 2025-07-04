using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class UserRoleMapper
{
    public static UserRoleDTO ToDTO(UserRole ur)
    {
        return new UserRoleDTO(
            userId: ur.UserId,
            roleId: ur.RoleId
        );
    }

    public static UserRole ToEntity(UserRoleDTO urDto)
    {
        return new UserRole(
            userId: urDto.UserId,
            roleId: urDto.RoleId
        );
    }
}