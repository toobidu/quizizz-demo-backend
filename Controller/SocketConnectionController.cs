using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class SocketConnectionController
{
    private readonly ISocketConnectionRepository _socketConnectionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ISocketConnectionService _socketService;

    public SocketConnectionController(
        ISocketConnectionRepository socketConnectionRepository,
        IUserRepository userRepository,
        IRoomRepository roomRepository,
        ISocketConnectionService socketService)
    {
        _socketConnectionRepository = socketConnectionRepository;
        _userRepository = userRepository;
        _roomRepository = roomRepository;
        _socketService = socketService;
    }

    public async Task<ApiResponse<SocketConnectionDTO>> RegisterConnectionAsync(string socketId, int? userId, string? roomCode)
    {
        try
        {
            // Kiểm tra user có tồn tại không (nếu có userId)
            if (userId.HasValue)
            {
                var user = await _userRepository.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    return ApiResponse<SocketConnectionDTO>.Fail("User không tồn tại");
                }
            }

            // Kiểm tra room có tồn tại không (nếu có roomCode)
            int? roomId = null;
            if (!string.IsNullOrEmpty(roomCode))
            {
                var room = await _roomRepository.GetRoomByCodeAsync(roomCode);
                if (room == null)
                {
                    return ApiResponse<SocketConnectionDTO>.Fail("Phòng không tồn tại");
                }
                roomId = room.Id;
            }

            // Tạo kết nối mới
            var now = DateTime.UtcNow;
            var connection = new SocketConnection
            {
                SocketId = socketId,
                UserId = userId,
                RoomId = roomId,
                ConnectedAt = now,
                LastActivity = now
            };

            await _socketConnectionRepository.CreateConnectionAsync(connection);

            var result = new SocketConnectionDTO
            {
                SocketId = connection.SocketId,
                UserId = connection.UserId,
                RoomId = connection.RoomId,
                ConnectedAt = connection.ConnectedAt
            };

            return ApiResponse<SocketConnectionDTO>.Success(result, "Đăng ký kết nối thành công", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<SocketConnectionDTO>.Fail("Lỗi khi đăng ký kết nối: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> UpdateConnectionRoomAsync(string socketId, string roomCode)
    {
        try
        {
            // Kiểm tra kết nối có tồn tại không
            var connection = await _socketConnectionRepository.GetConnectionBySocketIdAsync(socketId);
            if (connection == null)
            {
                return ApiResponse<object>.Fail("Kết nối không tồn tại");
            }

            // Kiểm tra room có tồn tại không
            var room = await _roomRepository.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                return ApiResponse<object>.Fail("Phòng không tồn tại");
            }

            // Cập nhật room cho kết nối
            connection.RoomId = room.Id;
            connection.LastActivity = DateTime.UtcNow;

            await _socketConnectionRepository.UpdateConnectionAsync(connection);

            return ApiResponse<object>.Success(new
            {
                socketId,
                roomId = room.Id,
                roomCode,
                updatedAt = connection.LastActivity
            }, "Cập nhật phòng cho kết nối thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi cập nhật phòng cho kết nối: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> DisconnectAsync(string socketId)
    {
        try
        {
            // Kiểm tra kết nối có tồn tại không
            var connection = await _socketConnectionRepository.GetConnectionBySocketIdAsync(socketId);
            if (connection == null)
            {
                return ApiResponse<object>.Fail("Kết nối không tồn tại");
            }

            // Xóa kết nối
            await _socketConnectionRepository.DeleteConnectionAsync(socketId);

            // Nếu user đang trong phòng, thông báo cho các user khác
            if (connection.RoomId.HasValue && connection.UserId.HasValue)
            {
                var room = await _roomRepository.GetRoomByIdAsync(connection.RoomId.Value);
                var user = await _userRepository.GetUserByIdAsync(connection.UserId.Value);

                if (room != null && user != null)
                {
                    await _socketService.BroadcastToRoomAsync(room.RoomCode, "player-left", new
                    {
                        userId = user.Id,
                        username = user.Username
                    });
                }
            }

            return ApiResponse<object>.Success(new
            {
                socketId,
                disconnectedAt = DateTime.UtcNow
            }, "Ngắt kết nối thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi ngắt kết nối: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetConnectionsByRoomAsync(string roomCode)
    {
        try
        {
            // Kiểm tra room có tồn tại không
            var room = await _roomRepository.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                return ApiResponse<object>.Fail("Phòng không tồn tại");
            }

            // Lấy danh sách kết nối trong phòng
            var connections = await _socketConnectionRepository.GetConnectionsByRoomIdAsync(room.Id);

            return ApiResponse<object>.Success(new
            {
                roomCode,
                roomId = room.Id,
                connections = connections.Select(c => new
                {
                    socketId = c.SocketId,
                    userId = c.UserId,
                    connectedAt = c.ConnectedAt,
                    lastActivity = c.LastActivity
                }).ToList(),
                totalConnections = connections.Count
            }, "Lấy danh sách kết nối thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy danh sách kết nối: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetConnectionsByUserAsync(int userId)
    {
        try
        {
            // Kiểm tra user có tồn tại không
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<object>.Fail("User không tồn tại");
            }

            // Lấy danh sách kết nối của user
            var connections = await _socketConnectionRepository.GetConnectionsByUserIdAsync(userId);

            return ApiResponse<object>.Success(new
            {
                userId,
                username = user.Username,
                connections = connections.Select(c => new
                {
                    socketId = c.SocketId,
                    roomId = c.RoomId,
                    connectedAt = c.ConnectedAt,
                    lastActivity = c.LastActivity
                }).ToList(),
                totalConnections = connections.Count
            }, "Lấy danh sách kết nối thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy danh sách kết nối: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> UpdateLastActivityAsync(string socketId)
    {
        try
        {
            // Kiểm tra kết nối có tồn tại không
            var connection = await _socketConnectionRepository.GetConnectionBySocketIdAsync(socketId);
            if (connection == null)
            {
                return ApiResponse<object>.Fail("Kết nối không tồn tại");
            }

            // Cập nhật thời gian hoạt động cuối
            connection.LastActivity = DateTime.UtcNow;
            await _socketConnectionRepository.UpdateConnectionAsync(connection);

            return ApiResponse<object>.Success(new
            {
                socketId,
                lastActivity = connection.LastActivity
            }, "Cập nhật thời gian hoạt động cuối thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi cập nhật thời gian hoạt động cuối: " + ex.Message);
        }
    }
}