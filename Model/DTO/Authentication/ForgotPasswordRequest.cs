using System.Text.RegularExpressions;
namespace ConsoleApp1.Model.DTO.Authentication;
public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public bool ValidField()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            return false;
        }
        // Basic email regex pattern
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        var trimmedEmail = Email.Trim();
        var isValid = Regex.IsMatch(trimmedEmail, emailPattern);
        return isValid;
    }
}
