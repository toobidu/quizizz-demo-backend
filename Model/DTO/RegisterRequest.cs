namespace ConsoleApp1.Model.DTO;

public class RegisterRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Email { get; set; }

    public RegisterRequest(string username, string password, string email)
    {
        Username = username;
        Password = password;
        Email = email;
    }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(Email);
    }
}