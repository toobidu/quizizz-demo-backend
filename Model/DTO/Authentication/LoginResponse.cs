namespace ConsoleApp1.Model.DTO.Authentication;

public class LoginResponse
{
    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }

    public LoginResponse(string accessToken, string refreshToken) =>
        (AccessToken, RefreshToken) = (accessToken, refreshToken);
}