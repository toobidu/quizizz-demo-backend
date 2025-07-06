using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class RankRepositoryImplement : IRankRepository
{
    public readonly string ConnectionString;

    public RankRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<Rank?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM ranks WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Rank>(query, new { Id = id });
    }

    public async Task<Rank?> GetByUserIdAsync(int userId)
    {
        const string query = @"SELECT * FROM ranks WHERE user_id = @UserId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Rank>(query, new { UserId = userId });
    }

    public async Task<int> AddAsync(Rank rank)
    {
        const string query = @"
            INSERT INTO ranks (user_id, total_score, games_played, updated_at) 
            VALUES (@UserId, @TotalScore, @GamesPlayed, @UpdatedAt) 
            RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, rank);
    }

    public async Task UpdateAsync(Rank rank)
    {
        const string query = @"
            UPDATE ranks 
            SET total_score = @TotalScore, 
                games_played = @GamesPlayed, 
                updated_at = @UpdatedAt 
            WHERE user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, rank);
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
}
