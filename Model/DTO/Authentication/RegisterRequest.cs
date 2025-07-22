using System.Text.Json.Serialization;
namespace ConsoleApp1.Model.DTO.Authentication;
public class RegisterRequest
{
    [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
    [JsonPropertyName("full_name")] public string FullName { get; set; } = string.Empty;
    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;
    [JsonPropertyName("phone_number")] public string PhoneNumber { get; set; } = string.Empty;
    [JsonPropertyName("address")] public string Address { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("confirm_password")] public string ConfirmPassword { get; set; } = string.Empty;
    public RegisterRequest()
    {
    }
    public RegisterRequest(string username, string fullName, string email, string phoneNumber, string address,
        string password, string confirmPassword)
    {
        Username = username;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
        Password = password;
        ConfirmPassword = confirmPassword;
    }
    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(PhoneNumber) &&
               !string.IsNullOrWhiteSpace(Address) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !string.IsNullOrWhiteSpace(ConfirmPassword) &&
               Password == ConfirmPassword;
    }
}
