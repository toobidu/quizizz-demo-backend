using ConsoleApp1.Model.DTO.Rooms;
namespace ConsoleApp1.Mapper.Rooms;
public static class LeaderboardMapper
{
    public static LeaderboardDTO ToDTO(List<RankDTO> ranks)
    {
        return new LeaderboardDTO(
            topPlayers: ranks
        );
    }
}
