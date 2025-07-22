using ConsoleApp1.Config;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class SocketConnectionController
{
    private readonly ISocketConnectionDbService _socketConnectionService;
    public SocketConnectionController(ISocketConnectionDbService socketConnectionService)
    {
        _socketConnectionService = socketConnectionService;
    }
    public async Task<ApiResponse<object>> GetByIdAsync(int id)
    {
        try
        {
            var socketConnection = await _socketConnectionService.GetByIdAsync(id);
            if (socketConnection == null)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections");
            return ApiResponse<object>.Success(socketConnection, "Socket connection retrieved successfully", 200, "/api/socket-connections");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections");
        }
    }
    public async Task<ApiResponse<object>> GetBySocketIdAsync(string socketId)
    {
        try
        {
            var socketConnection = await _socketConnectionService.GetBySocketIdAsync(socketId);
            if (socketConnection == null)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections/by-socket-id");
            return ApiResponse<object>.Success(socketConnection, "Socket connection retrieved successfully", 200, "/api/socket-connections/by-socket-id");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections/by-socket-id");
        }
    }
    public async Task<ApiResponse<object>> GetByRoomIdAsync(int roomId)
    {
        try
        {
            var socketConnections = await _socketConnectionService.GetByRoomIdAsync(roomId);
            return ApiResponse<object>.Success(socketConnections, "Socket connections retrieved successfully", 200, "/api/socket-connections/by-room");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections/by-room");
        }
    }
    public async Task<ApiResponse<object>> GetByUserIdAsync(int userId)
    {
        try
        {
            var socketConnections = await _socketConnectionService.GetByUserIdAsync(userId);
            return ApiResponse<object>.Success(socketConnections, "Socket connections retrieved successfully", 200, "/api/socket-connections/by-user");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections/by-user");
        }
    }
    public async Task<ApiResponse<object>> CreateAsync(SocketConnection socketConnection)
    {
        try
        {
            var id = await _socketConnectionService.CreateAsync(socketConnection);
            return ApiResponse<object>.Success(new { Id = id }, "Socket connection created successfully", 201, "/api/socket-connections");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections");
        }
    }
    public async Task<ApiResponse<object>> UpdateAsync(SocketConnection socketConnection)
    {
        try
        {
            var success = await _socketConnectionService.UpdateAsync(socketConnection);
            if (!success)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections");
            return ApiResponse<object>.Success(new { Success = true }, "Socket connection updated successfully", 200, "/api/socket-connections");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections");
        }
    }
    public async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        try
        {
            var success = await _socketConnectionService.DeleteAsync(id);
            if (!success)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections");
            return ApiResponse<object>.Success(new { Success = true }, "Socket connection deleted successfully", 200, "/api/socket-connections");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections");
        }
    }
    public async Task<ApiResponse<object>> DeleteBySocketIdAsync(string socketId)
    {
        try
        {
            var success = await _socketConnectionService.DeleteBySocketIdAsync(socketId);
            if (!success)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections/by-socket-id");
            return ApiResponse<object>.Success(new { Success = true }, "Socket connection deleted successfully", 200, "/api/socket-connections/by-socket-id");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections/by-socket-id");
        }
    }
    public async Task<ApiResponse<object>> UpdateLastActivityAsync(string socketId)
    {
        try
        {
            var success = await _socketConnectionService.UpdateLastActivityAsync(socketId);
            if (!success)
                return ApiResponse<object>.Fail("Socket connection not found", 404, "NOT_FOUND", "/api/socket-connections/activity");
            return ApiResponse<object>.Success(new { Success = true }, "Last activity updated successfully", 200, "/api/socket-connections/activity");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/socket-connections/activity");
        }
    }
}
