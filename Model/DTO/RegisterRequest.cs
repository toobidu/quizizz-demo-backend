namespace ConsoleApp1.Model.DTO;

public class RegisterRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(TypeAccount);
    }
    
    public RegisterRequest(string username, string password, string typeAccount) =>
        (Username, Password, TypeAccount) = (username, password, typeAccount);
}