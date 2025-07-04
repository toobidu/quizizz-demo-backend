namespace ConsoleApp1.Model.DTO;

public class RolePermissionDTO
{
    public int RoleId { get; set; }
    public int PermissionId { get; set; }
    
    public RolePermissionDTO(int roleId, int permissionId) =>
        (RoleId, PermissionId) = (roleId, permissionId);
}