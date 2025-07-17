using System.Data;
using Dapper;
using Npgsql;

namespace ConsoleApp1;

public static class DebugHelper
{
    public static async Task CheckRoomPlayersInDatabase(string connectionString, int roomId)
    {
        Console.WriteLine($"=== DEBUG: Kiểm tra bảng room_players cho roomId: {roomId} ===");
        
        using var conn = new NpgsqlConnection(connectionString);
        
        // Kiểm tra dữ liệu trong bảng room_players
        var query = @"
            SELECT rp.room_id, rp.user_id, u.username, rp.score, rp.created_at, rp.updated_at
            FROM room_players rp
            LEFT JOIN users u ON rp.user_id = u.id
            WHERE rp.room_id = @RoomId
            ORDER BY rp.created_at ASC";
            
        var players = await conn.QueryAsync(query, new { RoomId = roomId });
        
        Console.WriteLine($"Tìm thấy {players.Count()} người chơi trong bảng room_players:");
        foreach (var player in players)
        {
            Console.WriteLine($"  - UserId: {player.user_id}, Username: {player.username ?? "NULL"}, Score: {player.score}, CreatedAt: {player.created_at}");
        }
        
        // Kiểm tra thông tin phòng
        var roomQuery = @"SELECT id, room_code, room_name, owner_id, status FROM rooms WHERE id = @RoomId";
        var room = await conn.QuerySingleOrDefaultAsync(roomQuery, new { RoomId = roomId });
        
        if (room != null)
        {
            Console.WriteLine($"Thông tin phòng - Id: {room.id}, Mã: {room.room_code}, Tên: {room.room_name}, Chủ phòng: {room.owner_id}, Trạng thái: {room.status}");
        }
        else
        {
            Console.WriteLine($"Không tìm thấy phòng {roomId}!");
        }
        
        Console.WriteLine("=== END DEBUG ===");
    }
}