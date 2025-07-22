using ConsoleApp1.Model.DTO.Game;
using System.Collections.Concurrent;
using System.Net.WebSockets;
namespace ConsoleApp1.Service.Implement.Socket.Scoring;
/// <summary>
/// Factory để tạo và quản lý các thành phần dịch vụ tính điểm
/// </summary>
public class ScoringServiceFactory
{
    private static readonly Lazy<ScoringServiceFactory> _instance = new(() => new ScoringServiceFactory());
    public static ScoringServiceFactory Instance => _instance.Value;
    private readonly ConcurrentDictionary<string, GameRoom> _gameRooms;
    private readonly ConcurrentDictionary<string, WebSocket> _connections;
    private ScoringServiceFactory()
    {
        _gameRooms = new ConcurrentDictionary<string, GameRoom>();
        _connections = new ConcurrentDictionary<string, WebSocket>();
    }
    /// <summary>
    /// Tạo ScoringSessionManager
    /// </summary>
    public ScoringSessionManager CreateSessionManager()
    {
        return new ScoringSessionManager();
    }
    /// <summary>
    /// Tạo ScoreCalculator
    /// </summary>
    public ScoreCalculator CreateScoreCalculator()
    {
        return new ScoreCalculator();
    }
    /// <summary>
    /// Tạo ScoreFormatter
    /// </summary>
    public ScoreFormatter CreateScoreFormatter(ScoreCalculator scoreCalculator)
    {
        return new ScoreFormatter(scoreCalculator);
    }
    /// <summary>
    /// Tạo SocketMessageSender với các dictionary chia sẻ
    /// </summary>
    public SocketMessageSender CreateMessageSender()
    {
        return new SocketMessageSender(_gameRooms, _connections);
    }
    /// <summary>
    /// Tạo AchievementCalculator
    /// </summary>
    public AchievementCalculator CreateAchievementCalculator()
    {
        return new AchievementCalculator();
    }
    /// <summary>
    /// Tạo complete scoring service với tất cả dependencies
    /// </summary>
    public (ScoringSessionManager sessionManager, 
            ScoreCalculator scoreCalculator, 
            ScoreFormatter scoreFormatter, 
            SocketMessageSender messageSender) CreateScoringComponents()
    {
        var sessionManager = CreateSessionManager();
        var scoreCalculator = CreateScoreCalculator();
        var scoreFormatter = CreateScoreFormatter(scoreCalculator);
        var messageSender = CreateMessageSender();
        return (sessionManager, scoreCalculator, scoreFormatter, messageSender);
    }
    /// <summary>
    /// Lấy dictionary phòng game chia sẻ
    /// </summary>
    public ConcurrentDictionary<string, GameRoom> GetGameRooms()
    {
        return _gameRooms;
    }
    /// <summary>
    /// Lấy dictionary kết nối chia sẻ
    /// </summary>
    public ConcurrentDictionary<string, WebSocket> GetConnections()
    {
        return _connections;
    }
    /// <summary>
    /// Đăng ký game room
    /// </summary>
    public void RegisterGameRoom(string roomCode, GameRoom gameRoom)
    {
        _gameRooms.TryAdd(roomCode, gameRoom);
    }
    /// <summary>
    /// Đăng ký WebSocket connection
    /// </summary>
    public void RegisterConnection(string socketId, WebSocket socket)
    {
        _connections.TryAdd(socketId, socket);
    }
    /// <summary>
    /// Hủy đăng ký game room
    /// </summary>
    public void UnregisterGameRoom(string roomCode)
    {
        _gameRooms.TryRemove(roomCode, out var _);
    }
    /// <summary>
    /// Hủy đăng ký WebSocket connection
    /// </summary>
    public void UnregisterConnection(string socketId)
    {
        _connections.TryRemove(socketId, out var _);
    }
}
