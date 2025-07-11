using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Users;

namespace ConsoleApp1.Mapper.Users;

public static class UserProfileMapper
{
    public static UserProfileDTO ToProfileDTO(User user, int highestRank, TimeSpan fastestTime,
        int highestScore, string bestTopic)
    {
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, highestRank,
            fastestTime, highestScore, bestTopic, user.CreatedAt);
    }

    public static UserDTO ToBasicDTO(User user)
    {
        return new UserDTO(
            user.Id, user.Username, user.FullName,
            user.Email, user.TypeAccount, user.PhoneNumber,
            user.Address, user.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}