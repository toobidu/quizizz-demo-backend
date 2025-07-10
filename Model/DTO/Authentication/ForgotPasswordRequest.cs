using System.Text.RegularExpressions;

namespace ConsoleApp1.Model.DTO.Authentication;

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;

    public bool ValidField()
    {
        Console.WriteLine($"[VALIDATION] Starting validation for email: '{Email ?? "null"}'");
        Console.WriteLine($"[VALIDATION] Email length: {Email?.Length ?? 0}");
        
        if (string.IsNullOrWhiteSpace(Email))
        {
            Console.WriteLine($"[VALIDATION] Email is null or whitespace: '{Email}'");
            return false;
        }

        // Basic email regex pattern
        var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        var trimmedEmail = Email.Trim();
        var isValid = Regex.IsMatch(trimmedEmail, emailPattern);
        
        Console.WriteLine($"[VALIDATION] Original email: '{Email}'");
        Console.WriteLine($"[VALIDATION] Trimmed email: '{trimmedEmail}'");
        Console.WriteLine($"[VALIDATION] Pattern match result: {isValid}");
        
        return isValid;
    }
}