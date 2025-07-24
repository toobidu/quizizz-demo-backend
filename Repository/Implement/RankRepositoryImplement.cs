using System.Data;
using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;
namespace ConsoleApp1.Repository.Implement;
public class RankRepositoryImplement : IRankRepository
{
    private readonly DatabaseHelper _dbHelper;
    public RankRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    private IDbConnection CreateConnection() => _dbHelper.GetConnection();
    
    public async Task<List<Rank>> GetGlobalRanksAsync(int page, int limit, string level)
    {
        string levelFilter = string.IsNullOrEmpty(level) ? "" : "AND level = @Level";
        string query = $@"
            SELECT * FROM ranks 
            WHERE 1=1 {levelFilter}
            ORDER BY total_score DESC 
            LIMIT @Limit OFFSET @Offset";
        
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Rank>(query, new { 
            Level = level, 
            Limit = limit, 
            Offset = (page - 1) * limit 
        });
        return result.ToList();
    }
    
    public async Task<int> GetTotalRanksCountAsync(string level)
    {
        string levelFilter = string.IsNullOrEmpty(level) ? "" : "WHERE level = @Level";
        string query = $@"SELECT COUNT(*) FROM ranks {levelFilter}";
        
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, new { Level = level });
    }
    
    public async Task<List<Rank>> GetTopRanksAsync(int top)
    {
        const string query = @"
            SELECT * FROM ranks 
            ORDER BY total_score DESC 
            LIMIT @Top";
            
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Rank>(query, new { Top = top });
        return result.ToList();
    }
    
    public async Task<Rank?> GetRankByUserIdAsync(int userId)
    {
        const string query = @"SELECT * FROM ranks WHERE user_id = @UserId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Rank>(query, new { UserId = userId });
    }
    
    public async Task<int> GetUserGlobalRankPositionAsync(int userId)
    {
        const string query = @"
            SELECT COUNT(*) + 1 FROM ranks 
            WHERE total_score > (SELECT total_score FROM ranks WHERE user_id = @UserId)";
            
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, new { UserId = userId });
    }
    
    public async Task<string> CalculateUserLevelAsync(int totalScore)
    {
        // Đơn giản hóa tính toán level dựa trên điểm số
        if (totalScore < 1000) return "Beginner";
        if (totalScore < 5000) return "Intermediate";
        if (totalScore < 10000) return "Advanced";
        if (totalScore < 20000) return "Expert";
        return "Master";
    }
    
    public async Task<bool> UpdateRankAsync(Rank rank)
    {
        const string query = @"
            UPDATE ranks 
            SET total_score = @TotalScore, 
                games_played = @GamesPlayed, 
                updated_at = @UpdatedAt 
            WHERE user_id = @UserId";
            
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, rank);
        return affected > 0;
    }
    
    public async Task<int> CreateRankAsync(Rank rank)
    {
        const string query = @"
            INSERT INTO ranks (user_id, total_score, games_played) 
            VALUES (@UserId, @TotalScore, @GamesPlayed) 
            RETURNING id";
            
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, rank);
    }
    
    public async Task<List<object>> GetUserRankHistoryAsync(int userId, int page, int limit)
    {
        const string query = @"
            SELECT ua.created_at as Date, SUM(ua.is_correct::int * 100) as Score
            FROM user_answers ua
            WHERE ua.user_id = @UserId
            GROUP BY ua.created_at
            ORDER BY ua.created_at DESC
            LIMIT @Limit OFFSET @Offset";
            
        using var conn = CreateConnection();
        var result = await conn.QueryAsync(query, new { 
            UserId = userId, 
            Limit = limit, 
            Offset = (page - 1) * limit 
        });
        return result.ToList();
    }
    
    public async Task<int> GetUserRankHistoryCountAsync(int userId)
    {
        const string query = @"
            SELECT COUNT(DISTINCT created_at)
            FROM user_answers
            WHERE user_id = @UserId";
            
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, new { UserId = userId });
    }
    
    public async Task<double> GetAverageScoreAsync()
    {
        const string query = @"SELECT AVG(total_score) FROM ranks";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<double>(query);
    }
    
    public async Task<int> GetHighestScoreAsync()
    {
        const string query = @"SELECT MAX(total_score) FROM ranks";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query);
    }
    
    public async Task<Dictionary<string, int>> GetLevelDistributionAsync()
    {
        // Giả định có một cột level trong bảng ranks
        const string query = @"
            SELECT 
                CASE 
                    WHEN total_score < 1000 THEN 'Beginner'
                    WHEN total_score < 5000 THEN 'Intermediate'
                    WHEN total_score < 10000 THEN 'Advanced'
                    WHEN total_score < 20000 THEN 'Expert'
                    ELSE 'Master'
                END as level,
                COUNT(*) as count
            FROM ranks
            GROUP BY level";
            
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<(string Level, int Count)>(query);
        return result.ToDictionary(x => x.Level, x => x.Count);
    }
    
    public async Task<bool> UpdateUserLevelAsync(int userId, string newLevel, int experiencePoints)
    {
        // Giả định có một cột level và experience_points trong bảng ranks
        const string query = @"
            UPDATE ranks 
            SET total_score = total_score + @ExperiencePoints
            WHERE user_id = @UserId";
            
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { 
            UserId = userId, 
            ExperiencePoints = experiencePoints 
        });
        return affected > 0;
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<Rank?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM ranks WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Rank>(query, new { Id = id });
    }
    
    public async Task<Rank?> GetByUserIdAsync(int userId)
    {
        return await GetRankByUserIdAsync(userId);
    }
    
    public async Task<int> AddAsync(Rank rank)
    {
        return await CreateRankAsync(rank);
    }
    
    public async Task UpdateAsync(Rank rank)
    {
        await UpdateRankAsync(rank);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM ranks WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }
    
    public async Task<IEnumerable<Rank>> GetAllAsync()
    {
        const string query = @"SELECT * FROM ranks";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Rank>(query);
        return result.ToList();
    }
    
    public async Task<IEnumerable<Rank>> GetTopPlayersAsync(int limit)
    {
        return await GetTopRanksAsync(limit);
    }
    
    public async Task<(int TotalGames, double AverageScore)> GetUserStatsAsync(int userId)
    {
        const string query = @"
        SELECT games_played as TotalGames,
               CAST(total_score AS FLOAT) / NULLIF(games_played, 0) as AverageScore
        FROM ranks
        WHERE user_id = @UserId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<(int TotalGames, double AverageScore)>(
            query, new { UserId = userId });
    }
}
