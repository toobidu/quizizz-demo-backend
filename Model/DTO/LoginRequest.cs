namespace ConsoleApp1.Model.DTO;

public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }
    
    public LoginRequest(string username, string password) =>
        (Username, Password) = (username, password);    
}
