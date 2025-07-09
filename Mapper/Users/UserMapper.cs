using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class UserMapper
{
    public static UserDTO ToDTO(User user)
    {
        return new UserDTO(
            id: user.Id,
            username: user.Username,
            fullName: user.FullName,
            email: user.Email,
            phone: user.Phone,
            address: user.Address,
            password: user.Password,
            typeAccount: user.TypeAccount
        );
    }

    public static User ToEntity(UserDTO userDto)
    {
        return new User(
            username: userDto.Username,
            password: userDto.Password,
            fullName: userDto.FullName,
            email: userDto.Email,
            phone: userDto.Phone,
            address: userDto.Address,
            typeAccount: userDto.TypeAccount
        );
    }
}