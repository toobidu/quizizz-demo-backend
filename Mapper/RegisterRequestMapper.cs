using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class RegisterRequestMapper
{
    public static User ToEntity(RegisterRequest request)
    {
        return new User(
            id: 0,
            username: request.Username,
            password: request.Password,
            typeAccount: request.TypeAccount
        );
    }
}