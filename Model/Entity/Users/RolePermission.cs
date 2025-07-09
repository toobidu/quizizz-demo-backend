namespace ConsoleApp1.Model.Entity.Users;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public RolePermission() { }

    public RolePermission(int roleId, int permissionId, DateTime createdAt, DateTime updatedAt)
    {
        RoleId = roleId;
        PermissionId = permissionId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}