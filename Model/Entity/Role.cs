namespace ConsoleApp1.Model.Entity;

public class Role
{
    public int Id { get; set; }
    public string RoleName { get; set; }

    public Role(int id, string roleName) => (Id, RoleName) = (id, roleName);
}