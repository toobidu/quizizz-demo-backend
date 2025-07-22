using ConsoleApp1.Config;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class GameController
{
    private readonly ISocketService _socketService;
    private readonly IJoinRoomService _joinRoomService;
    public GameController(ISocketService socketService, IJoinRoomService joinRoomService)
    {
        _socketService = socketService;
        _joinRoomService = joinRoomService;
    }
    public async Task<ApiResponse<string>> StartGameAsync(string roomCode, int hostUserId)
    {
        try
        {
            Console.WriteLine($"🎮 [Backend HTTP API] StartGameAsync called: roomCode={roomCode}, hostUserId={hostUserId}");
            
            // Kiểm tra tham số đầu vào
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                return ApiResponse<string>.Fail("Mã phòng không hợp lệ", 400, "INVALID_ROOM_CODE", "/api/game/start");
            }
            if (hostUserId <= 0)
            {
                return ApiResponse<string>.Fail("ID host không hợp lệ", 400, "INVALID_HOST_ID", "/api/game/start");
            }
            // Kiểm tra phòng có tồn tại không
            var room = await _joinRoomService.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                return ApiResponse<string>.Fail("Phòng không tồn tại", 404, "ROOM_NOT_FOUND", "/api/game/start");
            }
            // Kiểm tra quyền host
            if (room.OwnerId != hostUserId)
            {
                return ApiResponse<string>.Fail("Chỉ host mới có thể bắt đầu game", 403, "UNAUTHORIZED", "/api/game/start");
            }
            // Kiểm tra trạng thái phòng
            if (room.Status != "waiting" && room.Status != "ready")
            {
                return ApiResponse<string>.Fail("Phòng không ở trạng thái sẵn sàng để bắt đầu game", 400, "INVALID_ROOM_STATUS", "/api/game/start");
            }
            // Bắt đầu game qua Socket.IO
            await _socketService.StartGameAsync(roomCode);
            return ApiResponse<string>.Success("Game đã bắt đầu", "Game started successfully", 200, "/api/game/start");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/game/start");
        }
    }
    public async Task<ApiResponse<string>> SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions)
    {
        try
        {
            await _socketService.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
            return ApiResponse<string>.Success("Câu hỏi đã được gửi", "Question sent", 200, "/api/game/question");
        }
        catch (Exception ex)
        {
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/game/question");
        }
    }
}
