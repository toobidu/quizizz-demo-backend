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

    public async Task<SocketConnection> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE id = @Id";

        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<SocketConnection>(sql, new { Id = id });
    }

    public async Task<SocketConnection> GetBySocketIdAsync(string socketId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE socket_id = @SocketId";

        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryFirstOrDefaultAsync<SocketConnection>(sql, new { SocketId = socketId });
    }

    public async Task<IEnumerable<SocketConnection>> GetByRoomIdAsync(int roomId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE room_id = @RoomId";

        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryAsync<SocketConnection>(sql, new { RoomId = roomId });
    }

    public async Task<IEnumerable<SocketConnection>> GetByUserIdAsync(int userId)
    {
        const string sql = @"
            SELECT * FROM socket_connections
            WHERE user_id = @UserId";

        using var connection = _databaseHelper.GetConnection();
        return await connection.QueryAsync<SocketConnection>(sql, new { UserId = userId });
    }

    public async Task<int> CreateAsync(SocketConnection socketConnection)
    {
        const string sql = @"
            INSERT INTO socket_connections (socket_id, user_id, room_id, connected_at, last_activity)
            VALUES (@SocketId, @UserId, @RoomId, @ConnectedAt, @LastActivity)
            RETURNING id";

        using var connection = _databaseHelper.GetConnection();
        return await connection.ExecuteScalarAsync<int>(sql, socketConnection);
    }

    public async Task<bool> UpdateAsync(SocketConnection socketConnection)
    {
        const string sql = @"
            UPDATE socket_connections
            SET user_id = @UserId,
                room_id = @RoomId,
                last_activity = @LastActivity
            WHERE socket_id = @SocketId";

        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, socketConnection);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = @"
            DELETE FROM socket_connections
            WHERE id = @Id";

        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteBySocketIdAsync(string socketId)
    {
        const string sql = @"
            DELETE FROM socket_connections
            WHERE socket_id = @SocketId";

        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SocketId = socketId });
        return rowsAffected > 0;
    }

    public async Task<bool> UpdateLastActivityAsync(string socketId)
    {
        const string sql = @"
            UPDATE socket_connections
            SET last_activity = CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Ho_Chi_Minh'
            WHERE socket_id = @SocketId";

        using var connection = _databaseHelper.GetConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { SocketId = socketId });
        return rowsAffected > 0;
    }
}