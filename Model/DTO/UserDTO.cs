namespace ConsoleApp1.Model.DTO;

public class UserDTO
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }

    public UserDTO(string username, string password, string typeAccount)
    {
        Username = username;
        Password = password;
        TypeAccount = typeAccount;
    }
}