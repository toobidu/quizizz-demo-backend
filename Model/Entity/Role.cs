namespace ConsoleApp1.Model.Entity;

public class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Role() { }

    public Role(int id, string roleName, string description, DateTime createdAt, DateTime updatedAt)
    {
        Id = id;
        RoleName = roleName;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}