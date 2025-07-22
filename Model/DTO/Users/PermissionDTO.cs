namespace ConsoleApp1.Model.DTO.Users;
public class PermissionDTO
{
    public int Id { get; set; }
    public string PermissionName { get; set; }
    public string Description { get; set; }
    public PermissionDTO(int id, string permissionName, string description) =>
        (Id, PermissionName, Description) = (id, permissionName, description);
}
