using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Mapper.Rooms;
public static class RankMapper
{
    public static RankDTO ToDTO(Rank rank, string username)
    {
        return new RankDTO(
            userId: rank.UserId,
            username: username,
            totalScore: rank.TotalScore,
            gamesPlayed: rank.GamesPlayed,
            updatedAt: rank.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        );
    }

    public static Rank ToEntity(RankDTO rankDto)
    {
        return new Rank(
            id: 0,
            userId: rankDto.UserId,
            totalScore: rankDto.TotalScore,
            gamesPlayed: rankDto.GamesPlayed,
            createdAt: DateTime.UtcNow, // Example value
            updatedAt: DateTime.Parse(rankDto.UpdatedAt)
        );
    }
}