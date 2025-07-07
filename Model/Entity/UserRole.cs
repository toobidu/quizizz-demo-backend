namespace ConsoleApp1.Model.Entity;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }

    public UserRole()
    {
    }

    public UserRole(int userId, int roleId) =>
        (UserId, RoleId) = (userId, roleId);
}