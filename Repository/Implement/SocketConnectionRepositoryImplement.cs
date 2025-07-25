using ConsoleApp1.Data;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using Dapper;
using System.Data;
namespace ConsoleApp1.Repository.Implement;
public class SocketConnectionRepositoryImplement : ISocketConnectionRepository
{
    private readonly DatabaseHelper _databaseHelper;
    public SocketConnectionRepositoryImplement(DatabaseHelper databaseHelper)
    {
        _databaseHelper = databaseHelper;
    }
    
    public async Task<int> CreateConnectionAsync(SocketConnection socketConnection)
    {
        const string sql = @"
            INSERT INTO socket_connections (socket_id, user_id, room_id, connected_at, last_activity)
            VALUES (@SocketId, @UserId, @RoomId, @ConnectedAt, @LastActivity)
            RETURNING id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, socketConnection);
    }
    
    public async Task<SocketConnection?> GetConnectionByIdAsync(int id)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<SocketConnection>(sql, new { Id = id });
    }
    
    public async Task<SocketConnection?> GetConnectionBySocketIdAsync(string socketId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE socket_id = @SocketId";
        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<SocketConnection>(sql, new { SocketId = socketId });
    }
    
    public async Task<List<SocketConnection>> GetConnectionsByRoomIdAsync(int roomId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE room_id = @RoomId";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<SocketConnection>(sql, new { RoomId = roomId });
        return result.ToList();
    }
    
    public async Task<List<SocketConnection>> GetConnectionsByUserIdAsync(int userId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE user_id = @UserId";
        using var connection = _databaseHelper.GetConnection();
        var result = await connection.QueryAsync<SocketConnection>(sql, new { UserId = userId });
        return result.ToList();
    }
    
    public async Task<bool> UpdateConnectionAsync(SocketConnection connection)
    {
        const string sql = @"
            UPDATE socket_connections
            SET user_id = @UserId,
                room_id = @RoomId,
                last_activity = @LastActivity
            WHERE socket_id = @SocketId";
        using var dbConnection = _databaseHelper.GetConnection();
        var rowsAffected = await dbConnection.ExecuteAsync(sql, connection);
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteConnectionAsync(string socketId)
    {
        const string sql = @"
            DELETE FROM socket_connections
            WHERE socket_id = @SocketId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SocketId = socketId });
        return rowsAffected > 0;
    }
    
    public async Task<bool> DeleteConnectionByIdAsync(int id)
    {
        const string sql = @"
            DELETE FROM socket_connections
            WHERE id = @Id";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
    
    // Các phương thức cũ giữ lại để tương thích
    public async Task<SocketConnection?> GetByIdAsync(int id)
    {
        return await GetConnectionByIdAsync(id);
    }
    
    public async Task<SocketConnection?> GetBySocketIdAsync(string socketId)
    {
        return await GetConnectionBySocketIdAsync(socketId);
    }
    
    public async Task<List<SocketConnection>> GetByRoomIdAsync(int roomId)
    {
        return await GetConnectionsByRoomIdAsync(roomId);
    }
    
    public async Task<List<SocketConnection>> GetByUserIdAsync(int userId)
    {
        return await GetConnectionsByUserIdAsync(userId);
    }
    
    public async Task<List<SocketConnection>> GetByRoomCodeAsync(string roomCode)
    {
        try
        {
            string sql = @"
                SELECT sc.* 
                FROM socket_connections sc
                JOIN rooms r ON sc.room_id = r.id
                WHERE r.room_code = @RoomCode";
            
            using var connection = _databaseHelper.GetConnection();
            var result = await connection.QueryAsync<SocketConnection>(sql, new { RoomCode = roomCode });
            return result.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionRepository] Error getting connections by room code {roomCode}: {ex.Message}");
            return new List<SocketConnection>();
        }
    }
    
    public async Task<int> CreateAsync(SocketConnection socketConnection)
    {
        return await CreateConnectionAsync(socketConnection);
    }
    
    public async Task<bool> UpdateAsync(SocketConnection socketConnection)
    {
        return await UpdateConnectionAsync(socketConnection);
    }
    
    public async Task<bool> DeleteAsync(int id)
    {
        return await DeleteConnectionByIdAsync(id);
    }
    
    public async Task<bool> DeleteBySocketIdAsync(string socketId)
    {
        return await DeleteConnectionAsync(socketId);
    }
    
    public async Task<bool> UpdateLastActivityAsync(string socketId)
    {
        const string sql = @"
            UPDATE socket_connections
            SET last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
            WHERE socket_id = @SocketId";
        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SocketId = socketId });
        return rowsAffected > 0;
    }

    // ✅ THÊM METHOD CẬP NHẬT ROOM_ID
    public async Task<bool> UpdateRoomIdAsync(string socketId, string roomCode)
    {
        try
        {
            const string sql = @"
                UPDATE socket_connections 
                SET room_id = (SELECT id FROM rooms WHERE room_code = @RoomCode),
                    last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
                WHERE socket_id = @SocketId";
            
            using var connection = _databaseHelper.GetConnection();
            var rowsAffected = await connection.ExecuteAsync(sql, new { 
                SocketId = socketId, 
                RoomCode = roomCode 
            });
            
            Console.WriteLine($"✅ [SocketConnectionRepository] Updated room_id for socket {socketId} to room {roomCode}. Rows affected: {rowsAffected}");
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [SocketConnectionRepository] Error updating room_id for socket {socketId}: {ex.Message}");
            return false;
        }
    }
}
