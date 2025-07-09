using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Mapper.Users;

public static class UserMapper
{
    public static UserDTO ToDTO(User user)
    {
        return new UserDTO(
            id: user.Id,
            username: user.Username,
            fullName: user.FullName,
            email: user.Email,
            phoneNumber: user.PhoneNumber,
            address: user.Address,
            password: user.Password,
            typeAccount: user.TypeAccount
        );
    }

    public static User ToEntity(UserDTO userDto)
    {
        return new User(
            id: userDto.Id,
            username: userDto.Username,
            password: userDto.Password,
            fullName: userDto.FullName,
            email: userDto.Email,
            phoneNumber: userDto.PhoneNumber,
            address: userDto.Address,
            typeAccount: userDto.TypeAccount,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
    }
}