namespace ConsoleApp1.Model.DTO.Authentication;

public class VerifyOtpRequest
{
    public string Email { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(Email) && 
               !string.IsNullOrWhiteSpace(OtpCode) && 
               OtpCode.Length == 6;
    }
}