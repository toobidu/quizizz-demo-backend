using System.Data;
using ConsoleApp1.Model.Entity;
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
        const string query = @"INSERT INTO rooms (room_code, room_name, is_private, owner_id) 
                               VALUES (@RoomCode, @RoomName, @IsPrivate, @OwnerId) 
                               RETURNING id";
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(query, room);
    }

    public async Task UpdateAsync(Room room)
    {
        const string query = @"UPDATE rooms SET room_name = @RoomName, is_private = @IsPrivate 
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
}