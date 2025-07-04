namespace ConsoleApp1.Model.DTO;

public class PermissionDTO
{
    public int Id { get; set; }
    public string PermissionName { get; set; }
    
    public PermissionDTO(int id, string permissionName) =>
        (Id, PermissionName) = (id, permissionName);
}