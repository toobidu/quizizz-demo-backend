namespace ConsoleApp1.Model.DTO;

public class UserRoleDTO
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    
    public UserRoleDTO(int userId, int roleId) =>
        (UserId, RoleId) = (userId, roleId);
}