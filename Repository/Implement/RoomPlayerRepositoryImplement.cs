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
        Console.WriteLine($"[ROOM_PLAYER_REPO] GetByRoomIdAsync called for roomId: {roomId}");
        const string query = @"SELECT * FROM room_players WHERE room_id = @RoomId ORDER BY created_at ASC";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<RoomPlayer>(query, new { RoomId = roomId });
        var players = result.ToList();
        Console.WriteLine($"[ROOM_PLAYER_REPO] Found {players.Count} players in room {roomId}");
        foreach (var player in players)
        {
            Console.WriteLine($"[ROOM_PLAYER_REPO] Player - UserId: {player.UserId}, Score: {player.Score}, CreatedAt: {player.CreatedAt}");
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
        Console.WriteLine($"[ROOM_PLAYER_REPO] Adding player - RoomId: {roomPlayer.RoomId}, UserId: {roomPlayer.UserId}");
        
        // Kiểm tra duplicate trước khi thêm
        var existing = await GetByUserIdAndRoomIdAsync(roomPlayer.UserId, roomPlayer.RoomId);
        if (existing != null)
        {
            Console.WriteLine($"[ROOM_PLAYER_REPO] Player already exists - RoomId: {roomPlayer.RoomId}, UserId: {roomPlayer.UserId}, skipping insert");
            return 0;
        }
        
        roomPlayer.CreatedAt = DateTime.UtcNow;
        roomPlayer.UpdatedAt = DateTime.UtcNow;
        const string query = @"INSERT INTO room_players (room_id, user_id, score, time_taken, created_at, updated_at) 
                               VALUES (@RoomId, @UserId, @Score, @TimeTaken, @CreatedAt, @UpdatedAt)"; 
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, roomPlayer);
        Console.WriteLine($"[ROOM_PLAYER_REPO] Player added successfully - RoomId: {roomPlayer.RoomId}, UserId: {roomPlayer.UserId}");
        return 1;
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
        Console.WriteLine($"[ROOM_PLAYER_REPO] Deleting player - RoomId: {roomId}, UserId: {userId}");
        
        // Kiểm tra trước khi xóa
        const string checkQuery = @"SELECT COUNT(*) FROM room_players WHERE user_id = @UserId AND room_id = @RoomId";
        using var conn = CreateConnection();
        var existsBefore = await conn.ExecuteScalarAsync<int>(checkQuery, new { UserId = userId, RoomId = roomId });
        Console.WriteLine($"[ROOM_PLAYER_REPO] Player exists before delete: {existsBefore > 0}");
        
        const string query = @"DELETE FROM room_players 
                               WHERE user_id = @UserId AND room_id = @RoomId";
        var affected = await conn.ExecuteAsync(query, new { UserId = userId, RoomId = roomId });
        
        // Kiểm tra sau khi xóa
        var existsAfter = await conn.ExecuteScalarAsync<int>(checkQuery, new { UserId = userId, RoomId = roomId });
        Console.WriteLine($"[ROOM_PLAYER_REPO] Delete result - RoomId: {roomId}, UserId: {userId}, Affected rows: {affected}, Still exists: {existsAfter > 0}");
        
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
