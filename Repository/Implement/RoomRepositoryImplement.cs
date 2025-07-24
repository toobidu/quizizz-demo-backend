using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;

namespace ConsoleApp1.Repository.Implement;

public class RoomRepositoryImplement : IRoomRepository
{
    private readonly DatabaseHelper _dbHelper;

    public RoomRepositoryImplement(DatabaseHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }
    
    // Các phương thức tương thích cũ
    public async Task<Room?> GetByIdAsync(int id)
    {
        return await GetRoomByIdAsync(id);
    }
    
    public async Task<Room?> GetByCodeAsync(string code)
    {
        return await GetRoomByCodeAsync(code);
    }
    
    public async Task<int> AddAsync(Room room)
    {
        return await CreateRoomAsync(room);
    }
    
    public async Task<bool> UpdateAsync(Room room)
    {
        return await UpdateRoomAsync(room);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        return await DeleteRoomAsync(id);
    }
    
    public async Task<bool> ExistsByCodeAsync(string code)
    {
        const string sql = "SELECT EXISTS (SELECT 1 FROM rooms WHERE room_code = @Code)";
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { Code = code });
    }
    
    public async Task<int> GetPlayerCountAsync(int roomId)
    {
        const string sql = "SELECT COUNT(*) FROM room_players WHERE room_id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { RoomId = roomId });
    }
    
    public async Task<bool> UpdateStatusAsync(int roomId, string status)
    {
        const string sql = "UPDATE rooms SET status = @Status, updated_at = CURRENT_TIMESTAMP WHERE id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { RoomId = roomId, Status = status });
        return rowsAffected > 0;
    }
    
    public async Task<bool> UpdateMaxPlayersAsync(int roomId, int maxPlayers)
    {
        const string sql = "UPDATE rooms SET max_players = @MaxPlayers, updated_at = CURRENT_TIMESTAMP WHERE id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { RoomId = roomId, MaxPlayers = maxPlayers });
        return rowsAffected > 0;
    }
    
    public async Task<string> GetRoomTopicNameAsync(int roomId)
    {
        const string sql = @"
            SELECT t.name 
            FROM topics t 
            JOIN room_settings rs ON rs.setting_value = t.id::text 
            WHERE rs.room_id = @RoomId AND rs.setting_key = 'topic_id'";
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<string>(sql, new { RoomId = roomId }) ?? "Unknown";
    }
    
    public async Task<int> GetRoomQuestionCountAsync(int roomId)
    {
        const string sql = @"
            SELECT COUNT(*) 
            FROM game_questions gq 
            JOIN game_sessions gs ON gq.game_session_id = gs.id 
            WHERE gs.room_id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { RoomId = roomId });
    }
    
    public async Task<int> GetRoomCountdownTimeAsync(int roomId)
    {
        const string sql = @"
            SELECT COALESCE(
                (SELECT setting_value::int FROM room_settings 
                WHERE room_id = @RoomId AND setting_key = 'countdown_time'), 
                30)";
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { RoomId = roomId });
    }
    
    public async Task<List<Room>> GetPublicWaitingRoomsAsync()
    {
        const string sql = "SELECT * FROM rooms WHERE is_private = false AND status = 'waiting'";
        using var connection = _dbHelper.GetConnection();
        var rooms = await connection.QueryAsync<Room>(sql);
        return rooms.ToList();
    }
    
    public async Task<List<Room>> GetAllRoomsWithDetailsAsync()
    {
        const string sql = @"
            SELECT r.*, u.username as OwnerUsername 
            FROM rooms r 
            JOIN users u ON r.owner_id = u.id";
        using var connection = _dbHelper.GetConnection();
        var rooms = await connection.QueryAsync<Room>(sql);
        return rooms.ToList();
    }

    public async Task<Room?> GetRoomByIdAsync(int roomId)
    {
        const string sql = "SELECT * FROM rooms WHERE id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<Room>(sql, new { RoomId = roomId });
    }

    public async Task<Room?> GetRoomByCodeAsync(string roomCode)
    {
        const string sql = "SELECT * FROM rooms WHERE room_code = @RoomCode";
        using var connection = _dbHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<Room>(sql, new { RoomCode = roomCode });
    }

    public async Task<List<Room>> GetRoomsByOwnerIdAsync(int ownerId)
    {
        const string sql = "SELECT * FROM rooms WHERE owner_id = @OwnerId";
        using var connection = _dbHelper.GetConnection();
        var rooms = await connection.QueryAsync<Room>(sql, new { OwnerId = ownerId });
        return rooms.ToList();
    }

    public async Task<int> CreateRoomAsync(Room room)
    {
        const string sql = @"
            INSERT INTO rooms (room_code, room_name, is_private, owner_id, status, max_players)
            VALUES (@RoomCode, @RoomName, @IsPrivate, @OwnerId, @Status, @MaxPlayers)
            RETURNING id";
        
        using var connection = _dbHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, room);
    }

    public async Task<bool> UpdateRoomAsync(Room room)
    {
        const string sql = @"
            UPDATE rooms
            SET room_name = @RoomName,
                is_private = @IsPrivate,
                status = @Status,
                max_players = @MaxPlayers,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @Id";
        
        using var connection = _dbHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, room);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteRoomAsync(int roomId)
    {
        const string sql = "DELETE FROM rooms WHERE id = @RoomId";
        using var connection = _dbHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { RoomId = roomId });
        return rowsAffected > 0;
    }
}