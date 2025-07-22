using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;
namespace ConsoleApp1.Mapper.Users;
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
            roleId: urDto.RoleId,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
    }
}
