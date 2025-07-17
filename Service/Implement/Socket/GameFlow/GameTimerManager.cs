using System.Collections.Concurrent;

namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Service quản lý bộ đếm thời gian cho game
/// </summary>
public class GameTimerManager
{
    private readonly ConcurrentDictionary<string, Timer> _gameTimers = new();
    private readonly ConcurrentDictionary<string, Timer> _countdownTimers = new();

    /// <summary>
    /// Tạo bộ đếm thời gian game
    /// </summary>
    public void CreateGameTimer(string roomCode, int timeLimit, Func<Task> onTimeoutCallback)
    {
        // Hủy timer cũ nếu có
        DisposeGameTimer(roomCode);

        var timer = new Timer(async _ =>
        {
            await onTimeoutCallback();
            DisposeGameTimer(roomCode);
        }, null, TimeSpan.FromSeconds(timeLimit), Timeout.InfiniteTimeSpan);

        _gameTimers[roomCode] = timer;
        Console.WriteLine($"[TIMER] Đã tạo bộ đếm thời gian game cho phòng {roomCode}: {timeLimit}s");
    }

    /// <summary>
    /// Tạo bộ đếm ngược
    /// </summary>
    public void CreateCountdownTimer(string roomCode, int startCount, Func<int, Task> onCountdownCallback, Func<Task> onFinishCallback)
    {
        // Hủy timer cũ nếu có
        DisposeCountdownTimer(roomCode);

        var currentCount = startCount;
        var timer = new Timer(async _ =>
        {
            if (currentCount > 0)
            {
                await onCountdownCallback(currentCount);
                currentCount--;
            }
            else
            {
                await onFinishCallback();
                DisposeCountdownTimer(roomCode);
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        _countdownTimers[roomCode] = timer;
        Console.WriteLine($"[TIMER] Đã tạo bộ đếm ngược cho phòng {roomCode}: {startCount}s");
    }

    /// <summary>
    /// Tạo bộ đếm định kỳ (ví dụ: cập nhật thời gian mỗi giây)
    /// </summary>
    public void CreatePeriodicTimer(string roomCode, int intervalSeconds, Func<Task> callback)
    {
        var timerId = $"{roomCode}_periodic";
        
        // Hủy timer cũ nếu có
        if (_gameTimers.TryRemove(timerId, out var oldTimer))
        {
            oldTimer.Dispose();
        }

        var timer = new Timer(async _ =>
        {
            await callback();
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));

        _gameTimers[timerId] = timer;
        Console.WriteLine($"[TIMER] Đã tạo bộ đếm định kỳ cho phòng {roomCode}: mỗi {intervalSeconds}s");
    }

    /// <summary>
    /// Dừng bộ đếm thời gian game
    /// </summary>
    public void DisposeGameTimer(string roomCode)
    {
        if (_gameTimers.TryRemove(roomCode, out var timer))
        {
            timer.Dispose();
            Console.WriteLine($"[TIMER] Đã dừng bộ đếm thời gian game cho phòng {roomCode}");
        }
    }

    /// <summary>
    /// Dừng bộ đếm ngược
    /// </summary>
    public void DisposeCountdownTimer(string roomCode)
    {
        if (_countdownTimers.TryRemove(roomCode, out var timer))
        {
            timer.Dispose();
            Console.WriteLine($"[TIMER] Đã dừng bộ đếm ngược cho phòng {roomCode}");
        }
    }

    /// <summary>
    /// Dừng tất cả bộ đếm thời gian cho phòng
    /// </summary>
    public void DisposeAllTimersForRoom(string roomCode)
    {
        DisposeGameTimer(roomCode);
        DisposeCountdownTimer(roomCode);
        
        // Hủy bộ đếm định kỳ
        var periodicTimerId = $"{roomCode}_periodic";
        if (_gameTimers.TryRemove(periodicTimerId, out var periodicTimer))
        {
            periodicTimer.Dispose();
            Console.WriteLine($"[TIMER] Đã dừng bộ đếm định kỳ cho phòng {roomCode}");
        }
    }

    /// <summary>
    /// Kiểm tra bộ đếm thời gian game có đang chạy không
    /// </summary>
    public bool HasActiveGameTimer(string roomCode)
    {
        return _gameTimers.ContainsKey(roomCode);
    }

    /// <summary>
    /// Kiểm tra bộ đếm ngược có đang chạy không
    /// </summary>
    public bool HasActiveCountdownTimer(string roomCode)
    {
        return _countdownTimers.ContainsKey(roomCode);
    }

    /// <summary>
    /// Lấy số lượng bộ đếm thời gian đang hoạt động
    /// </summary>
    public int GetActiveTimersCount()
    {
        return _gameTimers.Count + _countdownTimers.Count;
    }

    /// <summary>
    /// Dọn dẹp tất cả bộ đếm thời gian (khi tắt service)
    /// </summary>
    public void DisposeAllTimers()
    {
        foreach (var timer in _gameTimers.Values)
        {
            timer.Dispose();
        }
        _gameTimers.Clear();

        foreach (var timer in _countdownTimers.Values)
        {
            timer.Dispose();
        }
        _countdownTimers.Clear();

        Console.WriteLine("[TIMER] Đã dừng tất cả bộ đếm thời gian");
    }
}