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
            typeAccount: userDto.TypeAccount
        );
    }
}