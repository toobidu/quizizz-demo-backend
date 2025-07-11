using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO.Users;

public class UpdateProfileRequest
{
    [JsonPropertyName("fullName")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    public UpdateProfileRequest() { }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Email);
    }
}
