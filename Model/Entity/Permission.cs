namespace ConsoleApp1.Model.Entity;

public class Permission
{
    public int Id { get; set; }
    public string PermissionName { get; set; }
    
    public Permission(int id, string permissionName) =>
        (Id, PermissionName) = (id, permissionName);
}