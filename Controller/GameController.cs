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
            // Kiểm tra quyền host
            var room = await _joinRoomService.GetRoomByCodeAsync(roomCode);
            if (room == null)
                return ApiResponse<string>.Fail("Phòng không tồn tại", 404, "ROOM_NOT_FOUND", "/api/game/start");

            if (room.OwnerId != hostUserId)
                return ApiResponse<string>.Fail("Chỉ host mới có thể bắt đầu game", 403, "UNAUTHORIZED", "/api/game/start");

            // Bắt đầu game qua Socket.IO
            await _socketService.StartGameAsync(roomCode);
            
            return ApiResponse<string>.Success("Game đã bắt đầu", "Game started successfully", 200, "/api/game/start");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME_CONTROLLER] Start game error: {ex.Message}");
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
            Console.WriteLine($"[GAME_CONTROLLER] Send question error: {ex.Message}");
            return ApiResponse<string>.Fail("Lỗi máy chủ: " + ex.Message, 500, "SERVER_ERROR", "/api/game/question");
        }
    }
}