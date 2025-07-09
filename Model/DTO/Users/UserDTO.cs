namespace ConsoleApp1.Model.DTO;

public class UserDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }

    public UserDTO(int id, string username, string fullName, string email, string phone, string address, string password, string typeAccount) =>
        (Id, Username, FullName, Email, Phone, Address, Password, TypeAccount) = (id, username, fullName, email, phone, address, password, typeAccount);
}