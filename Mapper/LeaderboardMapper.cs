using ConsoleApp1.Model.DTO;

namespace ConsoleApp1.Mapper;

public static class LeaderboardMapper
{
    public static LeaderboardDTO ToDTO(List<RankDTO> ranks)
    {
        return new LeaderboardDTO(
            topPlayers: ranks
        );
    }
}