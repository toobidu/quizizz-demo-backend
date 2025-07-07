using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO;

public class LoginRequest
{
    public LoginRequest() { }

    public LoginRequest(string username, string password)
    {
        Username = username;
        Password = password;
    }

    [JsonPropertyName("username")]
    public string Username { get; set; }

    [JsonPropertyName("password")]
    public string Password { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }
}
