namespace ConsoleApp1.Model.DTO.Users;
public class RoleDTO
{
    public int Id { get; set; }
    public string RoleName { get; set; }
    public string Description { get; set; }
    public RoleDTO(int id, string roleName, string description) =>
        (Id, RoleName, Description) = (id, roleName, description);
}
