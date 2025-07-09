using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO;

public class RegisterRequest
{
    [JsonPropertyName("username")] public string Username { get; set; }
    [JsonPropertyName("full_name")] public string FullName { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("phone")] public string Phone { get; set; }
    [JsonPropertyName("address")] public string Address { get; set; }

    [JsonPropertyName("password")] public string Password { get; set; }

    public RegisterRequest()
    {
    }

    public RegisterRequest(string username, string fullName, string email, string phone, string address,
        string password)
    {
        Username = username;
        FullName = fullName;
        Email = email;
        Phone = phone;
        Address = address;
        Password = password;
    }


    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(Phone) &&
               !string.IsNullOrWhiteSpace(Address) &&
               !string.IsNullOrWhiteSpace(Password);
    }
}