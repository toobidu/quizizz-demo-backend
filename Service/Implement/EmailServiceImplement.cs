using ConsoleApp1.Config;
using ConsoleApp1.Service.Interface;
using System.Net;
using System.Net.Mail;
namespace ConsoleApp1.Service.Implement;
public class EmailServiceImplement : IEmailService
{
    private readonly EmailConfig _emailConfig;
    public EmailServiceImplement(EmailConfig emailConfig)
    {
        _emailConfig = emailConfig;
    }
    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName),
                Subject = "Mã OTP đặt lại mật khẩu",
                /*Body = CreateOtpEmailBody(otpCode),
                IsBodyHtml = true*/
            };
            mailMessage.To.Add(toEmail);
            using var smtpClient = new SmtpClient(_emailConfig.SmtpHost, _emailConfig.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailConfig.FromEmail, _emailConfig.FromPassword),
                EnableSsl = _emailConfig.EnableSsl
            };
            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string username)
    {
        try
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName),
                Subject = "Chào mừng bạn đến với Quizizz!",
                /*Body = CreateWelcomeEmailBody(username),
                IsBodyHtml = true*/
            };
            mailMessage.To.Add(toEmail);
            using var smtpClient = new SmtpClient(_emailConfig.SmtpHost, _emailConfig.SmtpPort)
            {
                Credentials = new NetworkCredential(_emailConfig.FromEmail, _emailConfig.FromPassword),
                EnableSsl = _emailConfig.EnableSsl
            };
            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
    /*private string CreateOtpEmailBody(string otpCode)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #333;'>Đặt lại mật khẩu Quizizz</h2>
                <p>Bạn đã yêu cầu đặt lại mật khẩu cho tài khoản Quizizz của mình.</p>
                <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; text-align: center; margin: 20px 0;'>
                    <h3 style='color: #007bff; font-size: 24px; margin: 0;'>Mã OTP của bạn:</h3>
                    <h1 style='color: #007bff; font-size: 36px; margin: 10px 0; letter-spacing: 5px;'>{otpCode}</h1>
                </div>
                <p><strong>Lưu ý:</strong></p>
                <ul>
                    <li>Mã OTP này có hiệu lực trong 5 phút</li>
                    <li>Không chia sẻ mã này với bất kỳ ai</li>
                    <li>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này</li>
                </ul>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>
                    Email này được gửi tự động từ hệ thống Quizizz. Vui lòng không trả lời email này.
                </p>
            </div>
        </body>
        </html>";
    }
    private string CreateWelcomeEmailBody(string username)
    {
        return $@"
        <html>
        <body style='font-family: Arial, sans-serif;'>
            <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                <h2 style='color: #333;'>Chào mừng {username} đến với Quizizz! 🎉</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản Quizizz. Bạn đã sẵn sàng để:</p>
                <ul>
                    <li>🎮 Tham gia các trò chơi quiz thú vị</li>
                    <li>🏆 Cạnh tranh với bạn bè</li>
                    <li>📚 Học tập qua các câu hỏi đa dạng</li>
                    <li>🎯 Nâng cao kiến thức của bản thân</li>
                </ul>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='#' style='background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>
                        Bắt đầu chơi ngay!
                    </a>
                </div>
                <p>Chúc bạn có những trải nghiệm tuyệt vời cùng Quizizz!</p>
                <hr style='margin: 30px 0;'>
                <p style='color: #666; font-size: 12px;'>
                    Đội ngũ Quizizz<br>
                    Email này được gửi tự động từ hệ thống.
                </p>
            </div>
        </body>
        </html>";
    }*/
}
