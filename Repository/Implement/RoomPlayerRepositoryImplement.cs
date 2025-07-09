using System.Data;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class RoomPlayerRepositoryImplement : IRoomPlayerRepository
{
    public readonly string ConnectionString;

    public RoomPlayerRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<RoomPlayer?> GetByUserIdAndRoomIdAsync(int userId, int roomId)
    {
        const string query = @"SELECT * FROM room_players 
                               WHERE user_id = @UserId AND room_id = @RoomId";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<RoomPlayer>(
            query, new { UserId = userId, RoomId = roomId });
    }

    public async Task<IEnumerable<RoomPlayer>> GetByRoomIdAsync(int roomId)
    {
        const string query = @"SELECT * FROM room_players WHERE room_id = @RoomId";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RoomPlayer>(query, new { RoomId = roomId });
        return result.ToList();
    }

    public async Task<int> AddAsync(RoomPlayer roomPlayer)
    {
        const string query = @"INSERT INTO room_players (room_id, user_id, score, time_taken) 
                               VALUES (@RoomId, @UserId, @Score, @TimeTaken) RETURNING id"; 
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, roomPlayer);
    }

    public async Task UpdateAsync(RoomPlayer roomPlayer)
    {
        const string query = @"UPDATE room_players 
                               SET score = @Score, time_taken = @TimeTaken 
                               WHERE room_id = @RoomId AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, roomPlayer);
    }

    public async Task<bool> DeleteByUserIdAndRoomIdAsync(int userId, int roomId)
    {
        const string query = @"DELETE FROM room_players 
                               WHERE user_id = @UserId AND room_id = @RoomId";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { UserId = userId, RoomId = roomId });
        return affected > 0;
    }
    
    public async Task UpdateTimeAndScoreAsync(int roomId, int userId, TimeSpan timeTaken, int score)
    {
        const string query = @"
        UPDATE room_players 
        SET time_taken = @TimeTaken, 
            score = @Score
        WHERE room_id = @RoomId 
        AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { RoomId = roomId, UserId = userId, TimeTaken = timeTaken, Score = score });
    }
}
