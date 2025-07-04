namespace ConsoleApp1.Model.DTO;

public class LeaderboardDTO
{
    public List<RankDTO> TopPlayers { get; set; }
    
    public LeaderboardDTO(List<RankDTO> topPlayers) =>
        TopPlayers = topPlayers;
}