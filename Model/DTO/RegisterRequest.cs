using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO;

public class RegisterRequest
{
    public RegisterRequest() { }
    
    public RegisterRequest(string username, string password, string typeAccount)
    {
        Username = username;
        Password = password;
        TypeAccount = typeAccount;
    }

    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("password")]
    public string Password { get; set; }
    
    [JsonPropertyName("typeAccount")]
    public string TypeAccount { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(TypeAccount);
    }
}