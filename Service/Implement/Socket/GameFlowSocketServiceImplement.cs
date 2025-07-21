using ConsoleApp1.Service.Interface.Socket;
using ConsoleApp1.Service.Implement.Socket.GameFlow;
using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ConsoleApp1.Service.Implement.Socket;

/// <summary>
/// Service handling game flow via WebSocket - Responsible for:
/// 1. Starting and ending games
/// 2. Sending questions and managing time
/// 3. Tracking player progress
/// 4. Updating game state
/// </summary>
public class GameFlowSocketServiceImplement : IGameFlowSocketService
{
    // Dictionary storing game rooms (shared with RoomManagementService)
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms = new();
    
    // Dictionary storing WebSocket connections (shared with ConnectionService)
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    // Main orchestrator component
    private readonly GameFlowOrchestrator _orchestrator;

    public GameFlowSocketServiceImplement()
    {
        _orchestrator = new GameFlowOrchestrator(_gameRooms, _connections);
    }

    /// <summary>
    /// Start a game in the room (simple version)
    /// </summary>
    /// <param name="roomCode">Room code to start the game</param>
    public async Task StartGameAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Starting simple game for room: {roomCode}");
        await _orchestrator.StartSimpleGameAsync(roomCode);
    }

    /// <summary>
    /// Start a game with a list of questions and time limit
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="questions">List of questions (JSON object)</param>
    /// <param name="gameTimeLimit">Time limit for the entire game (seconds)</param>
    public async Task StartGameWithQuestionsAsync(string roomCode, object questions, int gameTimeLimit)
    {
        Console.WriteLine($"[GAMEFLOW] Starting game with questions for room: {roomCode}, time: {gameTimeLimit}s");
        await _orchestrator.StartGameWithQuestionsAsync(roomCode, questions, gameTimeLimit);
    }

    /// <summary>
    /// Send the next question to a specific player
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="username">Username of the player to receive the question</param>
    public async Task SendNextQuestionToPlayerAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAMEFLOW] Sending next question to player {username} in room {roomCode}");
        await _orchestrator.SendNextQuestionToPlayerAsync(roomCode, username);
    }

    /// <summary>
    /// Send a question to all players in the room
    /// Used in synchronized mode (all players get the same question)
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="question">Question to send (JSON object)</param>
    /// <param name="questionIndex">Question order (starting from 0)</param>
    /// <param name="totalQuestions">Total number of questions</param>
    public async Task SendQuestionAsync(string roomCode, object question, int questionIndex, int totalQuestions)
    {
        Console.WriteLine($"[GAMEFLOW] Sending question {questionIndex + 1}/{totalQuestions} to room {roomCode}");
        await _orchestrator.SendQuestionAsync(roomCode, question, questionIndex, totalQuestions);
    }

    /// <summary>
    /// Send remaining game time update
    /// Called periodically by a timer
    /// </summary>
    /// <param name="roomCode">Room code</param>
    public async Task SendGameTimerUpdateAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Sending game timer update for room {roomCode}");
        await _orchestrator.SendGameTimeUpdateAsync(roomCode);
    }

    /// <summary>
    /// Get the progress of a specific player
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="username">Player's username</param>
    public async Task GetPlayerProgressAsync(string roomCode, string username)
    {
        Console.WriteLine($"[GAMEFLOW] Getting progress for player {username} in room {roomCode}");
        await _orchestrator.GetPlayerProgressAsync(roomCode, username);
    }

    /// <summary>
    /// Broadcast progress of all players
    /// For displaying realtime leaderboard
    /// </summary>
    /// <param name="roomCode">Room code</param>
    public async Task BroadcastPlayerProgressAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Broadcasting player progress for room {roomCode}");
        await _orchestrator.BroadcastPlayerProgressAsync(roomCode);
    }

    /// <summary>
    /// Clean up game session when it ends
    /// </summary>
    /// <param name="roomCode">Room code</param>
    public async Task CleanupGameSessionAsync(string roomCode)
    {
        Console.WriteLine($"[GAMEFLOW] Cleaning up game session for room {roomCode}");
        await _orchestrator.CleanupGameSessionAsync(roomCode);
    }

    /// <summary>
    /// Update game state
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="gameState">New state (waiting, countdown, playing, ended)</param>
    public async Task UpdateGameStateAsync(string roomCode, string gameState)
    {
        Console.WriteLine($"[GAMEFLOW] Updating game state for room {roomCode} to: {gameState}");
        await _orchestrator.UpdateGameStateAsync(roomCode, gameState);
    }

    /// <summary>
    /// Send countdown before starting the game
    /// </summary>
    /// <param name="roomCode">Room code</param>
    /// <param name="countdown">Countdown seconds (3, 2, 1, 0)</param>
    public async Task SendCountdownAsync(string roomCode, int countdown)
    {
        Console.WriteLine($"[GAMEFLOW] Sending countdown {countdown} seconds for room {roomCode}");
        await _orchestrator.SendCountdownAsync(roomCode, countdown);
    }
}