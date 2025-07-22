using System.Text.Json.Serialization;
namespace ConsoleApp1.Model.DTO.Authentication;
public class LoginRequest
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
    public LoginRequest() { }
    public LoginRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }
    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }
}
