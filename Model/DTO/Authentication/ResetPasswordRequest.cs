namespace ConsoleApp1.Model.DTO.Authentication;
public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(OtpCode) &&
               !string.IsNullOrWhiteSpace(NewPassword) &&
               OtpCode.Length == 6 &&
               NewPassword.Length >= 6;
    }
}
