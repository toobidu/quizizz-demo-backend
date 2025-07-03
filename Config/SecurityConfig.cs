namespace ConsoleApp1.Config;

public class SecurityConfig
{
    public string JwtKey { get; set; } = string.Empty;
    public string JwtIssuer { get; set; } = string.Empty;
    public string JwtAudience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}