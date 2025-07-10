namespace ConsoleApp1.Config;

public class EmailConfig
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = "";
    public string FromPassword { get; set; } = "";
    public string FromName { get; set; } = "Quizizz App";
    public bool EnableSsl { get; set; } = true;
}