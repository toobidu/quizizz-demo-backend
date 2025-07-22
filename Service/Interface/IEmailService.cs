namespace ConsoleApp1.Service.Interface;
public interface IEmailService
{
    Task<bool> SendOtpEmailAsync(string toEmail, string otpCode);
    Task<bool> SendWelcomeEmailAsync(string toEmail, string username);
}
