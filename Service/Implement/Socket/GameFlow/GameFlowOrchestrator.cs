using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ConsoleApp1.Service.Implement.Socket.GameFlow;

/// <summary>
/// Main orchestrator for game flow - Responsible for:
/// 1. Initializing and coordinating components
/// 2. Processing external requests
/// 3. Ensuring consistency between components
/// </summary>
public class GameFlowOrchestrator
{
    // Shared dictionaries
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;

    // Specialized components
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

        // Initialize components
        _sessionManager = new GameSessionManager();
        _timerManager = new GameTimerManager();
        _eventBroadcaster = new GameEventBroadcaster(_gameRooms, _connections);
        _questionManager = new GameQuestionManager(_sessionManager, _eventBroadcaster);
        _progressTracker = new GameProgressTracker(_sessionManager, _eventBroadcaster);
        _lifecycleManager = new GameLifecycleManager(_sessionManager, _timerManager, _eventBroadcaster, _gameRooms);
    }

    /// <summary>
    /// Start a simple game (without questions)
    /// </summary>
    public async Task StartSimpleGameAsync(string roomId)
    {
        Console.WriteLine($"[GAME] Starting simple game for room: {roomId}");
        
        try
        {
            await _lifecycleManager.BatDauGameDonGianAsync(roomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when starting simple game for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Start game with a list of questions
    /// </summary>
    public async Task StartGameWithQuestionsAsync(string roomId, object question, int timeLimit)
    {
        Console.WriteLine($"[GAME] Starting game with questions for room: {roomId}, time: {timeLimit}s");
        
        try
        {
            await _lifecycleManager.BatDauGameVoiCauHoiAsync(roomId, question, timeLimit);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when starting game with questions for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send next question to a specific player
    /// </summary>
    public async Task SendNextQuestionToPlayerAsync(string roomId, string playerName)
    {
        Console.WriteLine($"[GAME] Sending next question to {playerName} in room {roomId}");
        
        try
        {
            await _questionManager.GuiCauHoiTiepTheoChoNguoiChoiAsync(roomId, playerName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when sending question to {playerName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send question to all players
    /// </summary>
    public async Task SendQuestionAsync(string roomId, object question, int questionPosition, int totalQuestions)
    {
        Console.WriteLine($"[GAME] Sending question {questionPosition + 1}/{totalQuestions} to room {roomId}");
        
        try
        {
            await _questionManager.GuiCauHoiAsync(roomId, question, questionPosition, totalQuestions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when sending question to room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send game time update
    /// </summary>
    public async Task SendGameTimeUpdateAsync(string roomId)
    {
        try
        {
            await _progressTracker.GuiCapNhatThoiGianGameAsync(roomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when sending time update for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Get player progress
    /// </summary>
    public async Task GetPlayerProgressAsync(string roomId, string playerName)
    {
        Console.WriteLine($"[GAME] Getting progress for {playerName} in room {roomId}");
        
        try
        {
            await _progressTracker.LayTienDoNguoiChoiAsync(roomId, playerName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when getting progress for {playerName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcast progress of all players
    /// </summary>
    public async Task BroadcastPlayerProgressAsync(string roomId)
    {
        Console.WriteLine($"[GAME] Broadcasting player progress for room {roomId}");
        
        try
        {
            await _progressTracker.BroadcastTienDoNguoiChoiAsync(roomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when broadcasting progress for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cleanup game session
    /// </summary>
    public async Task CleanupGameSessionAsync(string roomId)
    {
        Console.WriteLine($"[GAME] Cleaning up game session for room {roomId}");
        
        try
        {
            await _lifecycleManager.DonDepGameSessionAsync(roomId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when cleaning up game session for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Update game state
    /// </summary>
    public async Task UpdateGameStateAsync(string roomId, string state)
    {
        Console.WriteLine($"[GAME] Updating game state for room {roomId} to: {state}");
        
        try
        {
            await _lifecycleManager.CapNhatTrangThaiGameAsync(roomId, state);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when updating game state for room {roomId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Send countdown
    /// </summary>
    public async Task SendCountdownAsync(string roomId, int countdown)
    {
        Console.WriteLine($"[GAME] Sending countdown {countdown} for room {roomId}");
        
        try
        {
            await _lifecycleManager.GuiDemNguocAsync(roomId, countdown);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GAME] Error when sending countdown for room {roomId}: {ex.Message}");
        }
    }
}