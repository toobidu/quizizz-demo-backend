namespace ConsoleApp1.Model.DTO;

public class RoleDTO
{
    public int Id { get; set; }
    public string RoleName { get; set; }
    
    public RoleDTO(int id, string roleName) =>
        (Id, RoleName) = (id, roleName);
}