namespace ConsoleApp1.Model.Entity.Users;

public class Permission
{
    public int Id { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Permission() { }

    public Permission(int id, string permissionName, string description, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        PermissionName = permissionName;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}