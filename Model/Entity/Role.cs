namespace ConsoleApp1.Model.Entity;

public class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; }
    public virtual ICollection<RolePermission> RolePermissions { get; set; }
}