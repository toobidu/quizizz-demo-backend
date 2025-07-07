using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class LoginRequestMapper
{
    public static User ToEntity(LoginRequest request)
    {
        return new User(
            username: request.Username,
            password: request.Password,
            typeAccount: "default"
        );
    }
}