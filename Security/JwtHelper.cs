using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ConsoleApp1.Config;
using Microsoft.IdentityModel.Tokens;

namespace ConsoleApp1.Security;

public class JwtHelper
{
    private readonly SecurityConfig config;
    private readonly byte[] key;

    public JwtHelper(SecurityConfig config)
    {
        this.config = config;
        key = Encoding.UTF8.GetBytes(config.JwtKey);
    }

    /// <summary>
    /// Sinh Access Token dạng JWT dựa vào thông tin người dùng.
    /// </summary>
    /// <param name="userId">ID người dùng.</param>
    /// <param name="username">Tên đăng nhập.</param>
    /// <param name="typeAccount">Loại tài khoản (Admin/Player).</param>
    /// <returns>Chuỗi JWT hợp lệ.</returns>
    public string GenerateAccessToken(int userId, string username, string typeAccount)
    {
        if (string.IsNullOrEmpty(config.JwtKey))
            throw new ArgumentException("SecretKey is not configured");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim("username", username),
            new Claim("typeAccount", typeAccount)
        };

        var token = new JwtSecurityToken(
            issuer: config.JwtIssuer,
            audience: config.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(config.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token, out SecurityToken? validatedToken)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = config.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = config.JwtAudience,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = true
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, parameters, out validatedToken);
            return principal;
        }
        catch
        {
            validatedToken = null;
            return null;
        }
    }

    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token, out _);
        var userIdClaim = principal?.FindFirst("userId");
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int id) ? id : null;
    }

    public string? GetUsernameFromToken(string token)
    {
        var principal = ValidateToken(token, out _);
        return principal?.FindFirst("username")?.Value;
    }

    public string? GetTypeAccountFromToken(string token)
    {
        var principal = ValidateToken(token, out _);
        return principal?.FindFirst("typeAccount")?.Value;
    }
}