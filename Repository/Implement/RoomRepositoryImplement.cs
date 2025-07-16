using System.Data;
using ConsoleApp1.Model.Entity;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
using Npgsql;

namespace ConsoleApp1.Repository.Implement;

public class RoomRepositoryImplement : IRoomRepository
{
    public readonly string ConnectionString;

    public RoomRepositoryImplement(string connectionString)
    {
        ConnectionString = connectionString;
    }

    private IDbConnection CreateConnection() => new NpgsqlConnection(ConnectionString);

    public async Task<Room?> GetByIdAsync(int id)
    {
        const string query = @"SELECT * FROM rooms WHERE id = @Id";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Room>(query, new { Id = id });
    }

    public async Task<Room?> GetByCodeAsync(string code)
    {
        const string query = @"SELECT * FROM rooms WHERE room_code = @Code";
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Room>(query, new { Code = code });
    }

    public async Task<int> AddAsync(Room room)
    {
        room.CreatedAt = DateTime.UtcNow;
        room.UpdatedAt = DateTime.UtcNow;
        
        const string query = @"INSERT INTO rooms (room_code, room_name, is_private, owner_id, status, max_players, created_at, updated_at) 
                               VALUES (@RoomCode, @RoomName, @IsPrivate, @OwnerId, @Status, @MaxPlayers, @CreatedAt, @UpdatedAt) 
                               RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, room);
    }

    public async Task UpdateAsync(Room room)
    {
        room.UpdatedAt = DateTime.UtcNow;
        
        const string query = @"UPDATE rooms SET room_name = @RoomName, is_private = @IsPrivate, 
                               owner_id = @OwnerId, status = @Status, max_players = @MaxPlayers,
                               updated_at = @UpdatedAt
                               WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, room);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string query = @"DELETE FROM rooms WHERE id = @Id";
        using var conn = CreateConnection();
        var affected = await conn.ExecuteAsync(query, new { Id = id });
        return affected > 0;
    }

    public async Task<IEnumerable<Room>> GetAllAsync()
    {
        const string query = @"SELECT * FROM rooms";
        using var conn = CreateConnection();
        var result = await conn.QueryAsync<Room>(query);
        return result.ToList();
    }

    public async Task<Room> UpdateStatusAsync(int roomId, string status)
    {
        const string query = @"UPDATE rooms 
            SET status = @Status, updated_at = @UpdatedAt 
            WHERE id = @Id
            RETURNING *";
        using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Room>(query, 
            new { Id = roomId, Status = status, UpdatedAt = DateTime.UtcNow });
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync()
    {
        const string query = @"SELECT * FROM rooms WHERE status = 'ACTIVE'";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Room>(query);
    }

    public async Task<int> GetPlayerCountAsync(int roomId)
    {
        const string query = @"SELECT COUNT(*) FROM room_players WHERE room_id = @RoomId";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(query, new { RoomId = roomId });
        Console.WriteLine($"[ROOM_REPO] Player count for room {roomId}: {count}");
        return count;
    }
    public async Task UpdateMaxPlayersAsync(int roomId, int maxPlayers)
    {
        const string query = @"
        UPDATE rooms 
        SET max_players = @MaxPlayers
        WHERE id = @Id";
        using var conn = CreateConnection();
        await conn.ExecuteAsync(query, new { Id = roomId, MaxPlayers = maxPlayers });
    }

    public async Task<IEnumerable<Room>> GetPublicWaitingRoomsAsync()
    {
        const string query = @"SELECT * FROM rooms WHERE is_private = false AND status = 'waiting'";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Room>(query);
    }

    public async Task<bool> ExistsByCodeAsync(string roomCode)
    {
        const string query = @"SELECT COUNT(*) FROM rooms WHERE room_code = @RoomCode";
        using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(query, new { RoomCode = roomCode });
        return count > 0;
    }

    public async Task<IEnumerable<Room>> GetAllRoomsWithDetailsAsync()
    {
        const string query = @"SELECT 
            r.id as Id,
            r.room_code as RoomCode,
            r.room_name as RoomName,
            r.is_private as IsPrivate,
            r.owner_id as OwnerId,
            r.status as Status,
            r.max_players as MaxPlayers,
            r.created_at as CreatedAt,
            r.updated_at as UpdatedAt
            FROM rooms r ORDER BY r.created_at DESC";
        using var conn = CreateConnection();
        return await conn.QueryAsync<Room>(query);
    }

    public async Task<string?> GetRoomTopicNameAsync(int roomId)
    {
        const string query = @"
            SELECT COALESCE(t.name, 'Kiến thức chung') 
            FROM room_settings rs 
            LEFT JOIN topics t ON t.id = CAST(rs.setting_value AS INTEGER)
            WHERE rs.room_id = @RoomId AND rs.setting_key = 'topic_id'
            LIMIT 1";
        using var conn = CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<string>(query, new { RoomId = roomId });
        return result ?? "Kiến thức chung";
    }

    public async Task<int> GetRoomQuestionCountAsync(int roomId)
    {
        const string query = @"
            SELECT COALESCE(CAST(rs.setting_value AS INTEGER), 10) 
            FROM room_settings rs 
            WHERE rs.room_id = @RoomId AND rs.setting_key = 'question_count'
            LIMIT 1";
        using var conn = CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<int?>(query, new { RoomId = roomId });
        return result ?? 10;
    }

    public async Task<int> GetRoomCountdownTimeAsync(int roomId)
    {
        const string query = @"
            SELECT COALESCE(CAST(rs.setting_value AS INTEGER), 300) 
            FROM room_settings rs 
            WHERE rs.room_id = @RoomId AND rs.setting_key = 'countdown_seconds'
            LIMIT 1";
        using var conn = CreateConnection();
        var result = await conn.QuerySingleOrDefaultAsync<int?>(query, new { RoomId = roomId });
        return result ?? 300;
    }
}