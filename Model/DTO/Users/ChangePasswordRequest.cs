namespace ConsoleApp1.Model.DTO.Users;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(CurrentPassword) && 
               !string.IsNullOrWhiteSpace(NewPassword) && 
               NewPassword.Length >= 6;
    }

    public ChangePasswordRequest(string currentPassword, string newPassword) =>
        (CurrentPassword, NewPassword) = (currentPassword, newPassword);
}