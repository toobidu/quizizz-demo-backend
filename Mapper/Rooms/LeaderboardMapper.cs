using ConsoleApp1.Model.DTO.Rooms;

namespace ConsoleApp1.Mapper.Rooms;

public static class LeaderboardMapper
{
    public static LeaderboardDTO ToLeaderboardDTO(dynamic data)
    {
        return new LeaderboardDTO
        {
            UserId = data.UserId,
            Username = data.Username,
            Score = data.Score,
            CorrectAnswers = data.CorrectAnswers,
            TotalAnswers = data.TotalAnswers,
            Rank = data.Rank
        };
    }
}