using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Rooms.Games;
using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;

namespace ConsoleApp1.Controller;

public class GameSessionController
{
    private readonly IGameSessionRepository _gameSessionRepository;
    private readonly IGameQuestionRepository _gameQuestionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ISocketConnectionService _socketService;

    public GameSessionController(
        IGameSessionRepository gameSessionRepository,
        IGameQuestionRepository gameQuestionRepository,
        IRoomRepository roomRepository,
        ISocketConnectionService socketService)
    {
        _gameSessionRepository = gameSessionRepository;
        _gameQuestionRepository = gameQuestionRepository;
        _roomRepository = roomRepository;
        _socketService = socketService;
    }

    public async Task<ApiResponse<GameSessionDTO>> CreateGameSessionAsync(string roomCode, int hostUserId)
    {
        try
        {
            // Kiểm tra phòng có tồn tại không
            var room = await _roomRepository.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                return ApiResponse<GameSessionDTO>.Fail("Phòng không tồn tại");
            }

            // Kiểm tra người dùng có phải là host không
            if (room.HostUserId != hostUserId)
            {
                return ApiResponse<GameSessionDTO>.Fail("Chỉ host mới có thể tạo phiên game");
            }

            // Tạo phiên game mới
            var gameSession = new GameSession
            {
                RoomId = room.Id,
                GameState = "waiting",
                CurrentQuestionIndex = 0,
                TimeLimit = 30, // Default time limit
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var sessionId = await _gameSessionRepository.CreateGameSessionAsync(gameSession);

            var result = new GameSessionDTO
            {
                Id = sessionId,
                RoomId = gameSession.RoomId,
                RoomCode = roomCode,
                GameState = gameSession.GameState,
                CurrentQuestionIndex = gameSession.CurrentQuestionIndex,
                TimeLimit = gameSession.TimeLimit,
                CreatedAt = gameSession.CreatedAt
            };

            return ApiResponse<GameSessionDTO>.Success(result, "Tạo phiên game thành công", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<GameSessionDTO>.Fail("Lỗi khi tạo phiên game: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> StartGameSessionAsync(int sessionId)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            // Cập nhật trạng thái và thời gian bắt đầu
            session.GameState = "playing";
            session.StartTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _gameSessionRepository.UpdateGameSessionAsync(session);

            // Lấy thông tin phòng
            var room = await _roomRepository.GetRoomByIdAsync(session.RoomId);

            // Gửi sự kiện WebSocket thông báo game bắt đầu
            if (room != null)
            {
                await _socketService.BroadcastToRoomAsync(room.RoomCode, "game-started", new
                {
                    roomCode = room.RoomCode,
                    gameSessionId = session.Id,
                    startTime = session.StartTime,
                    totalQuestions = await _gameQuestionRepository.GetQuestionCountForSessionAsync(sessionId),
                    timeLimit = session.TimeLimit
                });
            }

            return ApiResponse<object>.Success(new
            {
                sessionId,
                gameState = session.GameState,
                startTime = session.StartTime,
                message = "Game đã bắt đầu"
            }, "Bắt đầu phiên game thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi bắt đầu phiên game: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> EndGameSessionAsync(int sessionId)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            // Cập nhật trạng thái và thời gian kết thúc
            session.GameState = "ended";
            session.EndTime = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _gameSessionRepository.UpdateGameSessionAsync(session);

            // Lấy thông tin phòng
            var room = await _roomRepository.GetRoomByIdAsync(session.RoomId);

            // Gửi sự kiện WebSocket thông báo game kết thúc
            if (room != null)
            {
                // Lấy leaderboard và thống kê
                var leaderboard = await _gameSessionRepository.GetLeaderboardForSessionAsync(sessionId);
                var stats = await _gameSessionRepository.GetGameStatsForSessionAsync(sessionId);

                await _socketService.BroadcastToRoomAsync(room.RoomCode, "game-ended", new
                {
                    leaderboard,
                    gameStats = stats
                });
            }

            return ApiResponse<object>.Success(new
            {
                sessionId,
                gameState = session.GameState,
                endTime = session.EndTime,
                duration = session.EndTime - session.StartTime,
                message = "Game đã kết thúc"
            }, "Kết thúc phiên game thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi kết thúc phiên game: " + ex.Message);
        }
    }

    public async Task<ApiResponse<GameSummaryDTO>> GetGameSummaryAsync(int sessionId)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<GameSummaryDTO>.Fail("Phiên game không tồn tại");
            }

            // Lấy thông tin tổng kết game
            var leaderboard = await _gameSessionRepository.GetLeaderboardForSessionAsync(sessionId);
            var stats = await _gameSessionRepository.GetGameStatsForSessionAsync(sessionId);
            var questions = await _gameQuestionRepository.GetQuestionsForSessionAsync(sessionId);

            var summary = new GameSummaryDTO
            {
                SessionId = sessionId,
                RoomId = session.RoomId,
                StartTime = session.StartTime,
                EndTime = session.EndTime,
                Duration = session.EndTime.HasValue && session.StartTime.HasValue 
                    ? session.EndTime.Value - session.StartTime.Value 
                    : TimeSpan.Zero,
                TotalQuestions = questions.Count,
                Leaderboard = leaderboard,
                Stats = stats
            };

            return ApiResponse<GameSummaryDTO>.Success(summary, "Lấy tổng kết game thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<GameSummaryDTO>.Fail("Lỗi khi lấy tổng kết game: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> AddQuestionsToSessionAsync(int sessionId, List<int> questionIds)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            // Thêm các câu hỏi vào phiên game
            for (int i = 0; i < questionIds.Count; i++)
            {
                var gameQuestion = new GameQuestion
                {
                    GameSessionId = sessionId,
                    QuestionId = questionIds[i],
                    QuestionOrder = i,
                    TimeLimit = session.TimeLimit,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _gameQuestionRepository.AddQuestionToSessionAsync(gameQuestion);
            }

            return ApiResponse<object>.Success(new
            {
                sessionId,
                questionsAdded = questionIds.Count,
                message = "Đã thêm câu hỏi vào phiên game"
            }, "Thêm câu hỏi thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi thêm câu hỏi vào phiên game: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetCurrentQuestionAsync(int sessionId)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            var currentQuestion = await _gameQuestionRepository.GetQuestionByOrderAsync(sessionId, session.CurrentQuestionIndex);
            if (currentQuestion == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy câu hỏi hiện tại");
            }

            return ApiResponse<object>.Success(currentQuestion, "Lấy câu hỏi hiện tại thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy câu hỏi hiện tại: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> MoveToNextQuestionAsync(int sessionId)
    {
        try
        {
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(sessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            // Tăng chỉ số câu hỏi hiện tại
            session.CurrentQuestionIndex++;
            session.UpdatedAt = DateTime.UtcNow;

            // Kiểm tra xem đã hết câu hỏi chưa
            var totalQuestions = await _gameQuestionRepository.GetQuestionCountForSessionAsync(sessionId);
            if (session.CurrentQuestionIndex >= totalQuestions)
            {
                // Đã hết câu hỏi, kết thúc game
                session.GameState = "ended";
                session.EndTime = DateTime.UtcNow;
                
                await _gameSessionRepository.UpdateGameSessionAsync(session);
                
                // Lấy thông tin phòng
                var room = await _roomRepository.GetRoomByIdAsync(session.RoomId);
                
                // Gửi sự kiện WebSocket thông báo game kết thúc
                if (room != null)
                {
                    // Lấy leaderboard và thống kê
                    var leaderboard = await _gameSessionRepository.GetLeaderboardForSessionAsync(sessionId);
                    var stats = await _gameSessionRepository.GetGameStatsForSessionAsync(sessionId);
                    
                    await _socketService.BroadcastToRoomAsync(room.RoomCode, "game-ended", new
                    {
                        leaderboard,
                        gameStats = stats
                    });
                }
                
                return ApiResponse<object>.Success(new
                {
                    sessionId,
                    gameState = session.GameState,
                    message = "Game đã kết thúc, đã hết câu hỏi"
                }, "Game đã kết thúc");
            }
            
            await _gameSessionRepository.UpdateGameSessionAsync(session);
            
            // Lấy câu hỏi tiếp theo
            var nextQuestion = await _gameQuestionRepository.GetQuestionByOrderAsync(sessionId, session.CurrentQuestionIndex);
            
            // Lấy thông tin phòng
            var roomInfo = await _roomRepository.GetRoomByIdAsync(session.RoomId);
            
            // Gửi câu hỏi tiếp theo qua WebSocket
            if (roomInfo != null && nextQuestion != null)
            {
                await _socketService.BroadcastToRoomAsync(roomInfo.RoomCode, "question", new
                {
                    questionId = nextQuestion.QuestionId,
                    questionIndex = session.CurrentQuestionIndex,
                    totalQuestions,
                    questionText = nextQuestion.Question.QuestionText,
                    options = nextQuestion.Question.Answers.Select(a => a.AnswerText).ToList(),
                    timeLimit = nextQuestion.TimeLimit,
                    questionType = nextQuestion.Question.QuestionType.Name
                });
            }
            
            return ApiResponse<object>.Success(new
            {
                sessionId,
                currentQuestionIndex = session.CurrentQuestionIndex,
                totalQuestions,
                message = "Đã chuyển sang câu hỏi tiếp theo"
            }, "Chuyển câu hỏi thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi chuyển câu hỏi: " + ex.Message);
        }
    }
}