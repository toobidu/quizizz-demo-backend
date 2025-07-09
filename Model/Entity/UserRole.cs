namespace ConsoleApp1.Model.Entity;

public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserRole() { }

    public UserRole(int userId, int roleId, DateTime createdAt, DateTime updatedAt)
    {
        UserId = userId;
        RoleId = roleId;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}