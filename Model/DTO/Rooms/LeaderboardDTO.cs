namespace ConsoleApp1.Model.DTO.Rooms;
public class LeaderboardDTO
{
    public List<RankDTO> TopPlayers { get; set; }
    public LeaderboardDTO(List<RankDTO> topPlayers) =>
        TopPlayers = topPlayers;
}
