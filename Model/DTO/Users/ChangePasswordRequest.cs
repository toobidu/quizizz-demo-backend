using System.Text.Json.Serialization;

namespace ConsoleApp1.Model.DTO.Users;

public class ChangePasswordRequest
{
    [JsonPropertyName("currentPassword")]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [JsonPropertyName("newPassword")]
    public string NewPassword { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword) && 
               !string.IsNullOrWhiteSpace(NewPassword) && 
               NewPassword.Length >= 6;
    }

    public ChangePasswordRequest() { }
    
    public ChangePasswordRequest(string currentPassword, string newPassword) =>
        (CurrentPassword, NewPassword) = (currentPassword, newPassword);
}