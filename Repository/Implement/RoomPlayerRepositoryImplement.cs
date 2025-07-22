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
        const string query = @"SELECT * FROM room_players WHERE room_id = @RoomId ORDER BY created_at ASC";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RoomPlayer>(query, new { RoomId = roomId });
        var players = result.ToList();
        foreach (var player in players)
        {
        }
        return players;
    }
    public async Task<Room?> GetActiveRoomByUserIdAsync(int userId)
    {
        const string query = @"SELECT r.* FROM rooms r 
                               INNER JOIN room_players rp ON r.id = rp.room_id 
                               WHERE rp.user_id = @UserId AND r.status IN ('waiting', 'active')";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Room>(query, new { UserId = userId });
    }
    public async Task<int> AddAsync(RoomPlayer roomPlayer)
    {
        // Ki?m tra duplicate tru?c khi thêm
        var existing = await GetByUserIdAndRoomIdAsync(roomPlayer.UserId, roomPlayer.RoomId);
        if (existing != null)
        {
            return 0;
        }
        roomPlayer.CreatedAt = DateTime.UtcNow;
        roomPlayer.UpdatedAt = DateTime.UtcNow;
        const string query = @"INSERT INTO room_players (room_id, user_id, score, time_taken, status, socket_id, last_activity, created_at, updated_at) 
                               VALUES (@RoomId, @UserId, @Score, @TimeTaken, @Status, @SocketId, @LastActivity, @CreatedAt, @UpdatedAt)"; 
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, roomPlayer);
        return 1;
    }
    public async Task UpdateAsync(RoomPlayer roomPlayer)
    {
        const string query = @"UPDATE room_players 
                               SET score = @Score, time_taken = @TimeTaken, 
                                   status = @Status, socket_id = @SocketId, 
                                   last_activity = @LastActivity 
                               WHERE room_id = @RoomId AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, roomPlayer);
    }
    public async Task<bool> DeleteByUserIdAndRoomIdAsync(int userId, int roomId)
    {
        // Ki?m tra tru?c khi xóa
        const string checkQuery = @"SELECT COUNT(*) FROM room_players WHERE user_id = @UserId AND room_id = @RoomId";
        using var conn = CreateConnection();
        var existsBefore = await conn.ExecuteScalarAsync<int>(checkQuery, new { UserId = userId, RoomId = roomId });
        const string query = @"DELETE FROM room_players 
                               WHERE user_id = @UserId AND room_id = @RoomId";
        var affected = await conn.ExecuteAsync(query, new { UserId = userId, RoomId = roomId });
        // Ki?m tra sau khi xóa
        var existsAfter = await conn.ExecuteScalarAsync<int>(checkQuery, new { UserId = userId, RoomId = roomId });
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
    public async Task UpdatePlayerStatusAsync(int roomId, int userId, string status)
    {
        const string query = @"
        UPDATE room_players 
        SET status = @Status,
            last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'
        WHERE room_id = @RoomId 
        AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { RoomId = roomId, UserId = userId, Status = status });
    }
    public async Task UpdateSocketIdAsync(int roomId, int userId, string socketId)
    {
        const string query = @"
        UPDATE room_players 
        SET socket_id = @SocketId,
            last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'
        WHERE room_id = @RoomId 
        AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { RoomId = roomId, UserId = userId, SocketId = socketId });
    }
    public async Task UpdateLastActivityAsync(int roomId, int userId)
    {
        const string query = @"
        UPDATE room_players 
        SET last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'
        WHERE room_id = @RoomId 
        AND user_id = @UserId";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, 
            new { RoomId = roomId, UserId = userId });
    }
    public async Task<IEnumerable<RoomPlayer>> GetBySocketIdAsync(string socketId)
    {
        const string query = @"SELECT * FROM room_players WHERE socket_id = @SocketId";
        using var conn = CreateConnection();
        return await conn.QueryAsync<RoomPlayer>(query, new { SocketId = socketId });
    }
}
