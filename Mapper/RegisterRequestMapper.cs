using ConsoleApp1.Model.DTO.Authentication;
using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Mapper;

public static class RegisterRequestMapper
{
    public static User ToEntity(RegisterRequest request)
    {
        return new User(
            id: 0, // Default value
            username: request.Username,
            fullName: request.FullName,
            email: request.Email,
            phoneNumber: request.PhoneNumber,
            address: request.Address,
            password: request.Password,
            typeAccount: string.Empty,
            createdAt: DateTime.UtcNow, 
            updatedAt: DateTime.UtcNow
        );
    }
}