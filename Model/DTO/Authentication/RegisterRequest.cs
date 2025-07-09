using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO.Authentication;

public class RegisterRequest
{
    [JsonPropertyName("username")] public string Username { get; set; }
    [JsonPropertyName("full_name")] public string FullName { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("phone_number")] public string PhoneNumber { get; set; }
    [JsonPropertyName("address")] public string Address { get; set; }

    [JsonPropertyName("password")] public string Password { get; set; }

    public RegisterRequest()
    {
    }

    public RegisterRequest(string username, string fullName, string email, string phoneNumber, string address,
        string password)
    {
        Username = username;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
        Password = password;
    }


    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(PhoneNumber) &&
               !string.IsNullOrWhiteSpace(Address) &&
               !string.IsNullOrWhiteSpace(Password);
    }
}