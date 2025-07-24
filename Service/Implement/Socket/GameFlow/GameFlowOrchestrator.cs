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
        try
        {
            await _lifecycleManager.StartSimpleGameAsync(roomId);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Start game with a list of questions
    /// </summary>
    public async Task StartGameWithQuestionsAsync(string roomId, object question, int timeLimit)
    {
        try
        {
            await _lifecycleManager.StartGameWithQuestionsAsync(roomId, question, timeLimit);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Send next question to a specific player
    /// </summary>
    public async Task SendNextQuestionToPlayerAsync(string roomId, string playerName)
    {
        try
        {
            await _questionManager.SendNextQuestionToPlayerAsync(roomId, playerName);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Send question to all players
    /// </summary>
    public async Task SendQuestionAsync(string roomId, object question, int questionPosition, int totalQuestions)
    {
        try
        {
            await _questionManager.SendQuestionAsync(roomId, question, questionPosition, totalQuestions);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Send game time update
    /// </summary>
    public async Task SendGameTimeUpdateAsync(string roomId)
    {
        try
        {
            await _progressTracker.SendGameTimeUpdateAsync(roomId);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Get player progress
    /// </summary>
    public async Task GetPlayerProgressAsync(string roomId, string playerName)
    {
        try
        {
            await _progressTracker.GetPlayerProgressAsync(roomId, playerName);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Broadcast progress of all players
    /// </summary>
    public async Task BroadcastPlayerProgressAsync(string roomId)
    {
        try
        {
            await _progressTracker.BroadcastPlayerProgressAsync(roomId);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Cleanup game session
    /// </summary>
    public async Task CleanupGameSessionAsync(string roomId)
    {
        try
        {
            await _lifecycleManager.CleanupGameSessionAsync(roomId);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Update game state
    /// </summary>
    public async Task UpdateGameStateAsync(string roomId, string state)
    {
        try
        {
            await _lifecycleManager.UpdateGameStateAsync(roomId, state);
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>
    /// Send countdown
    /// </summary>
    public async Task SendCountdownAsync(string roomId, int countdown)
    {
        try
        {
            await _lifecycleManager.SendCountdownAsync(roomId, countdown);
        }
        catch (Exception ex)
        {
        }
    }
}
