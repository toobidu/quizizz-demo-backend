using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Điều phối chính cho luồng game - Chịu trách nhiệm:
/// 1. Khởi tạo và điều phối các component
/// 2. Xử lý các yêu cầu từ bên ngoài
/// 3. Đảm bảo tính nhất quán giữa các component
/// </summary>
public class GameFlowOrchestrator
{
    // Các dictionary được chia sẻ
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;

    // Các component chuyên biệt
    private readonly GameSessionManager _sessionManager;
    private readonly GameTimerManager _timerManager;
    private readonly GameEventBroadcaster _eventBroadcaster;
    private readonly GameQuestionManager _questionManager;
    private readonly GameProgressTracker _progressTracker;
    private readonly GameLifecycleManager _lifecycleManager;

    public GameFlowOrchestrator(
        ConcurrentDictionary<string, GameRoom> gameRooms,
        ConcurrentDictionary<string, WebSocket> connections)
    {
        _gameRooms = gameRooms;
        _connections = connections;

        // Khởi tạo các component
        _sessionManager = new GameSessionManager();
        _timerManager = new GameTimerManager();
        _eventBroadcaster = new GameEventBroadcaster(_gameRooms, _connections);
        _questionManager = new GameQuestionManager(_sessionManager, _eventBroadcaster);
        _progressTracker = new GameProgressTracker(_sessionManager, _eventBroadcaster);
        _lifecycleManager = new GameLifecycleManager(_sessionManager, _timerManager, _eventBroadcaster, _gameRooms);
    }

    /// <summary>
    /// Bắt đầu game đơn giản (không có câu hỏi)
    /// </summary>
    public async Task BatDauGameDonGianAsync(string maPhong)
    {
        Console.WriteLine($"[GAME] Đang bắt đầu game đơn giản cho phòng: {maPhong}");
        
        try
        {
            await _lifecycleManager.BatDauGameDonGianAsync(maPhong);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi bắt đầu game đơn giản cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Bắt đầu game với danh sách câu hỏi
    /// </summary>
    public async Task BatDauGameVoiCauHoiAsync(string maPhong, object cauHoi, int thoiGianGioiHan)
    {
        Console.WriteLine($"[GAME] Đang bắt đầu game với câu hỏi cho phòng: {maPhong}, thời gian: {thoiGianGioiHan}s");
        
        try
        {
            await _lifecycleManager.BatDauGameVoiCauHoiAsync(maPhong, cauHoi, thoiGianGioiHan);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi bắt đầu game với câu hỏi cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi câu hỏi tiếp theo cho người chơi cụ thể
    /// </summary>
    public async Task GuiCauHoiTiepTheoChoNguoiChoiAsync(string maPhong, string tenNguoiChoi)
    {
        Console.WriteLine($"[GAME] Đang gửi câu hỏi tiếp theo cho {tenNguoiChoi} trong phòng {maPhong}");
        
        try
        {
            await _questionManager.GuiCauHoiTiepTheoChoNguoiChoiAsync(maPhong, tenNguoiChoi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi gửi câu hỏi cho {tenNguoiChoi}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi câu hỏi đến tất cả người chơi
    /// </summary>
    public async Task GuiCauHoiAsync(string maPhong, object cauHoi, int viTriCauHoi, int tongSoCauHoi)
    {
        Console.WriteLine($"[GAME] Đang gửi câu hỏi {viTriCauHoi + 1}/{tongSoCauHoi} đến phòng {maPhong}");
        
        try
        {
            await _questionManager.GuiCauHoiAsync(maPhong, cauHoi, viTriCauHoi, tongSoCauHoi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi gửi câu hỏi đến phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi cập nhật thời gian game
    /// </summary>
    public async Task GuiCapNhatThoiGianGameAsync(string maPhong)
    {
        try
        {
            await _progressTracker.GuiCapNhatThoiGianGameAsync(maPhong);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi gửi cập nhật thời gian cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Lấy tiến độ người chơi
    /// </summary>
    public async Task LayTienDoNguoiChoiAsync(string maPhong, string tenNguoiChoi)
    {
        Console.WriteLine($"[GAME] Đang lấy tiến độ cho {tenNguoiChoi} trong phòng {maPhong}");
        
        try
        {
            await _progressTracker.LayTienDoNguoiChoiAsync(maPhong, tenNguoiChoi);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi lấy tiến độ cho {tenNguoiChoi}: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcast tiến độ tất cả người chơi
    /// </summary>
    public async Task BroadcastTienDoNguoiChoiAsync(string maPhong)
    {
        Console.WriteLine($"[GAME] Đang broadcast tiến độ người chơi cho phòng {maPhong}");
        
        try
        {
            await _progressTracker.BroadcastTienDoNguoiChoiAsync(maPhong);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi broadcast tiến độ cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Dọn dẹp game session
    /// </summary>
    public async Task DonDepGameSessionAsync(string maPhong)
    {
        Console.WriteLine($"[GAME] Đang dọn dẹp game session cho phòng {maPhong}");
        
        try
        {
            await _lifecycleManager.DonDepGameSessionAsync(maPhong);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi dọn dẹp game session cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cập nhật trạng thái game
    /// </summary>
    public async Task CapNhatTrangThaiGameAsync(string maPhong, string trangThai)
    {
        Console.WriteLine($"[GAME] Đang cập nhật trạng thái game cho phòng {maPhong} thành: {trangThai}");
        
        try
        {
            await _lifecycleManager.CapNhatTrangThaiGameAsync(maPhong, trangThai);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi cập nhật trạng thái game cho phòng {maPhong}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gửi đếm ngược
    /// </summary>
    public async Task GuiDemNguocAsync(string maPhong, int demNguoc)
    {
        Console.WriteLine($"[GAME] Đang gửi đếm ngược {demNguoc} cho phòng {maPhong}");
        
        try
        {
            await _lifecycleManager.GuiDemNguocAsync(maPhong, demNguoc);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Lỗi khi gửi đếm ngược cho phòng {maPhong}: {ex.Message}");
        }
    }
}