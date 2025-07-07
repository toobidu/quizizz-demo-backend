namespace ConsoleApp1.Model.Entity;

public class RolePermission
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public RolePermission()
    {
    }

    public RolePermission(int roleId, int permissionId) =>
        (RoleId, PermissionId) = (roleId, permissionId);
}