using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.GameFlow;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service xử lý luồng game qua WebSocket - Chịu trách nhiệm:
/// 1. Bắt đầu và kết thúc game
/// 2. Gửi câu hỏi và quản lý thời gian
/// 3. Theo dõi tiến độ người chơi
/// 4. Cập nhật trạng thái game
/// </summary>
public class GameFlowSocketServiceImplement : IGameFlowSocketService
{
    // Dictionary lưu trữ các phòng game (chia sẻ với RoomManagementService)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary lưu trữ các kết nối WebSocket (chia sẻ với ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    // Component điều phối chính
    private readonly GameFlowOrchestrator _orchestrator;

    public GameFlowSocketServiceImplement()
    {
        _orchestrator = new GameFlowOrchestrator(_gameRooms, _connections);
    }

    /// <summary>
    /// Bắt đầu game trong phòng (phiên bản đơn giản)
    /// </summary>
    /// <param name="roomCode">Mã phòng cần bắt đầu game</param>
    public async Task StartGameAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Đang bắt đầu trò chơi đơn giản cho phòng: {roomCode}");
        await _orchestrator.BatDauGameDonGianAsync(roomCode);
    }

    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi và thời gian giới hạn
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="questions">Danh sách câu hỏi (JSON object)</param>
    /// <param name="gameTimeLimit">Thời gian giới hạn cho toàn bộ game (giây)</param>
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit)
    {
        Console.WriteLine($"[GAMEFLOW] Đang bắt đầu trò chơi với câu hỏi cho phòng: {roomCode}, thời gian: {gameTimeLimit}s");
        await _orchestrator.BatDauGameVoiCauHoiAsync(roomCode, questions, gameTimeLimit);
    }

    /// <summary>
    /// Gửi câu hỏi tiếp theo cho một người chơi cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi cần nhận câu hỏi</param>
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAMEFLOW] Đang gửi câu hỏi tiếp theo cho người chơi {username} trong phòng {roomCode}");
        await _orchestrator.GuiCauHoiTiepTheoChoNguoiChoiAsync(roomCode, username);
    }

    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi trong phòng
    /// Dùng trong chế độ synchronized (tất cả cùng câu hỏi)
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="question">Câu hỏi cần gửi (JSON object)</param>
    /// <param name="questionIndex">Thứ tự câu hỏi (bắt đầu từ 0)</param>
    /// <param name="totalQuestions">Tổng số câu hỏi</param>
    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions)
    {
        Console.WriteLine($"[GAMEFLOW] Đang gửi câu hỏi {questionIndex + 1}/{totalQuestions} tới phòng {roomCode}");
        await _orchestrator.GuiCauHoiAsync(roomCode, question, questionIndex, totalQuestions);
    }

    /// <summary>
    /// Gửi cập nhật thời gian game còn lại
    /// Được gọi định kỳ bởi timer
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task SendGameTimerUpdateAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Đang gửi cập nhật thời gian trò chơi cho phòng {roomCode}");
        await _orchestrator.GuiCapNhatThoiGianGameAsync(roomCode);
    }

    /// <summary>
    /// Lấy tiến độ của một người chơi cụ thể
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="username">Tên người chơi</param>
    public async Task GetPlayerProgressAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAMEFLOW] Đang lấy tiến độ cho người chơi {username} trong phòng {roomCode}");
        await _orchestrator.LayTienDoNguoiChoiAsync(roomCode, username);
    }

    /// <summary>
    /// Broadcast tiến độ của tất cả người chơi
    /// Để hiển thị realtime leaderboard
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task BroadcastPlayerProgressAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Đang phát sóng tiến độ người chơi cho phòng {roomCode}");
        await _orchestrator.BroadcastTienDoNguoiChoiAsync(roomCode);
    }

    /// <summary>
    /// Dọn dẹp game session khi kết thúc
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    public async Task CleanupGameSessionAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Đang dọn dẹp phiên trò chơi cho phòng {roomCode}");
        await _orchestrator.DonDepGameSessionAsync(roomCode);
    }

    /// <summary>
    /// Cập nhật trạng thái game
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="gameState">Trạng thái mới (waiting, countdown, playing, ended)</param>
    public async Task UpdateGameStateAsync(string roomCode, string gameState)
    {
        Console.WriteLine($"[GAMEFLOW] Đang cập nhật trạng thái trò chơi cho phòng {roomCode} thành: {gameState}");
        await _orchestrator.CapNhatTrangThaiGameAsync(roomCode, gameState);
    }

    /// <summary>
    /// Gửi đếm ngược trước khi bắt đầu game
    /// </summary>
    /// <param name="roomCode">Mã phòng</param>
    /// <param name="countdown">Số giây đếm ngược (3, 2, 1, 0)</param>
    public async Task SendCountdownAsync(string roomCode, int countdown)
    {
        Console.WriteLine($"[GAMEFLOW] Đang gửi đếm ngược {countdown} giây cho phòng {roomCode}");
        await _orchestrator.GuiDemNguocAsync(roomCode, countdown);
    }
}