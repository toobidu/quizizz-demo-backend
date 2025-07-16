using System.Data;
using Dapper;
using Npgsql;

namespace ConsoleApp1;

public static class DebugHelper
{
    public static async Task CheckRoomPlayersInDatabase(string connectionString, int roomId)
    {
        Console.WriteLine($"=== DEBUG: Checking room_players table for roomId: {roomId} ===");
        
        using var conn = new NpgsqlConnection(connectionString);
        
        // Kiểm tra dữ liệu trong bảng room_players
        var query = @"
            SELECT rp.room_id, rp.user_id, u.username, rp.score, rp.created_at, rp.updated_at
            FROM room_players rp
            LEFT JOIN users u ON rp.user_id = u.id
            WHERE rp.room_id = @RoomId
            ORDER BY rp.created_at ASC";
            
        var players = await conn.QueryAsync(query, new { RoomId = roomId });
        
        Console.WriteLine($"Found {players.Count()} players in room_players table:");
        foreach (var player in players)
        {
            Console.WriteLine($"  - UserId: {player.user_id}, Username: {player.username ?? "NULL"}, Score: {player.score}, CreatedAt: {player.created_at}");
        }
        
        // Kiểm tra thông tin phòng
        var roomQuery = @"SELECT id, room_code, room_name, owner_id, status FROM rooms WHERE id = @RoomId";
        var room = await conn.QuerySingleOrDefaultAsync(roomQuery, new { RoomId = roomId });
        
        if (room != null)
        {
            Console.WriteLine($"Room info - Id: {room.id}, Code: {room.room_code}, Name: {room.room_name}, OwnerId: {room.owner_id}, Status: {room.status}");
        }
        else
        {
            Console.WriteLine($"Room {roomId} not found!");
        }
        
        Console.WriteLine("=== END DEBUG ===");
    }
}