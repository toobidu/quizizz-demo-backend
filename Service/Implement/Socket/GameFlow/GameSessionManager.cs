using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
namespace ConsoleApp1.Service.Implement.Socket.GameFlow;
/// <summary>
/// Service quản lý các phiên game
/// </summary>
public class GameSessionManager
{
    private readonly ConcurrentDictionary<string, GameSession> _gameSessions = new();
    /// <summary>
    /// Tạo phiên game mới với câu hỏi
    /// </summary>
    public GameSession CreateGameSession(string roomCode, List<QuestionData> questions, int timeLimit)
    {
        var gameSession = new GameSession
        {
            RoomCode = roomCode,
            Questions = questions,
            GameTimeLimit = timeLimit,
            GameStartTime = DateTime.UtcNow,
            IsGameActive = true
        };
        _gameSessions[roomCode] = gameSession;
        return gameSession;
    }
    /// <summary>
    /// Tạo phiên game đơn giản (không có câu hỏi)
    /// </summary>
    public GameSession CreateSimpleGameSession(string roomCode)
    {
        var gameSession = new GameSession
        {
            RoomCode = roomCode,
            GameStartTime = DateTime.UtcNow,
            IsGameActive = true
        };
        _gameSessions[roomCode] = gameSession;
        return gameSession;
    }
    /// <summary>
    /// Lấy phiên game theo mã phòng
    /// </summary>
    public GameSession? GetGameSession(string roomCode)
    {
        _gameSessions.TryGetValue(roomCode, out var session);
        return session;
    }
    /// <summary>
    /// Kiểm tra phiên game có đang hoạt động không
    /// </summary>
    public bool HasActiveGameSession(string roomCode)
    {
        return _gameSessions.TryGetValue(roomCode, out var session) && session.IsGameActive;
    }
    /// <summary>
    /// Khởi tạo tiến độ cho tất cả người chơi
    /// </summary>
    public void InitializePlayerProgress(string roomCode, List<GamePlayer> players)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
            return;
        foreach (var player in players)
        {
            gameSession.PlayerProgress[player.Username] = new PlayerGameProgress
            {
                Username = player.Username
            };
        }
    }
    /// <summary>
    /// Cập nhật tiến độ người chơi
    /// </summary>
    public void UpdatePlayerProgress(string roomCode, string username, Action<PlayerGameProgress> updateAction)
    {
        if (!_gameSessions.TryGetValue(roomCode, out var gameSession))
            return;
        if (gameSession.PlayerProgress.TryGetValue(username, out var progress))
        {
            updateAction(progress);
        }
    }
    /// <summary>
    /// Kết thúc phiên game
    /// </summary>
    public void EndGameSession(string roomCode)
    {
        if (_gameSessions.TryGetValue(roomCode, out var gameSession))
        {
            gameSession.IsGameActive = false;
            gameSession.IsGameEnded = true;
        }
    }
    /// <summary>
    /// Dọn dẹp phiên game
    /// </summary>
    public void CleanupGameSession(string roomCode)
    {
        if (_gameSessions.TryRemove(roomCode, out var gameSession))
        {
            // Dừng tất cả timer
            gameSession.GameTimer?.Dispose();
            gameSession.CountdownTimer?.Dispose();
        }
    }
    /// <summary>
    /// Lấy tất cả phiên game đang hoạt động
    /// </summary>
    public List<GameSession> GetActiveSessions()
    {
        return _gameSessions.Values.Where(s => s.IsGameActive).ToList();
    }
    /// <summary>
    /// Lấy thống kê phiên game
    /// </summary>
    public object GetSessionStatistics()
    {
        var tongPhienGame = _gameSessions.Count;
        var phienGameDangHoatDong = _gameSessions.Values.Count(s => s.IsGameActive);
        var phienGameDaKetThuc = _gameSessions.Values.Count(s => s.IsGameEnded);
        return new
        {
            tongPhienGame,
            phienGameDangHoatDong,
            phienGameDaKetThuc,
            trungBinhNguoiChoiMoiPhien = _gameSessions.Values
                .Where(s => s.PlayerProgress.Count > 0)
                .DefaultIfEmpty()
                .Average(s => s?.PlayerProgress.Count ?? 0)
        };
    }
}
