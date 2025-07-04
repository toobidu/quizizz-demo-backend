using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

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
            updatedAt: DateTime.Parse(rankDto.UpdatedAt)
        );
    }
}