using ConsoleApp1.Model.Entity.Rooms;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Controller;
public class GameSessionController
{
    private readonly IGameSessionService _gameSessionService;
    public GameSessionController(IGameSessionService gameSessionService)
    {
        _gameSessionService = gameSessionService;
    }
    public async Task<ApiResponse<object>> GetByIdAsync(int id)
    {
        try
        {
            var gameSession = await _gameSessionService.GetByIdAsync(id);
            if (gameSession == null)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND", "/api/game-sessions");
            return ApiResponse<object>.Success(gameSession, "Game session retrieved successfully", 200,
                "/api/game-sessions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/game-sessions");
        }
    }
    public async Task<ApiResponse<object>> GetByRoomIdAsync(int roomId)
    {
        try
        {
            var gameSession = await _gameSessionService.GetByRoomIdAsync(roomId);
            if (gameSession == null)
                return ApiResponse<object>.Fail("Game session not found for this room", 404, "NOT_FOUND",
                    "/api/game-sessions/by-room");
            return ApiResponse<object>.Success(gameSession, "Game session retrieved successfully", 200,
                "/api/game-sessions/by-room");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/by-room");
        }
    }
    public async Task<ApiResponse<object>> CreateAsync(GameSession gameSession)
    {
        try
        {
            var id = await _gameSessionService.CreateAsync(gameSession);
            return ApiResponse<object>.Success(new { Id = id }, "Game session created successfully", 201,
                "/api/game-sessions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/game-sessions");
        }
    }
    public async Task<ApiResponse<object>> UpdateAsync(GameSession gameSession)
    {
        try
        {
            var success = await _gameSessionService.UpdateAsync(gameSession);
            if (!success)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND", "/api/game-sessions");
            return ApiResponse<object>.Success(new { Success = true }, "Game session updated successfully", 200,
                "/api/game-sessions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/game-sessions");
        }
    }
    public async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        try
        {
            var success = await _gameSessionService.DeleteAsync(id);
            if (!success)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND", "/api/game-sessions");
            return ApiResponse<object>.Success(new { Success = true }, "Game session deleted successfully", 200,
                "/api/game-sessions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR", "/api/game-sessions");
        }
    }
    public async Task<ApiResponse<object>> UpdateGameStateAsync(int id, string gameState)
    {
        try
        {
            var success = await _gameSessionService.UpdateGameStateAsync(id, gameState);
            if (!success)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND", "/api/game-sessions/state");
            return ApiResponse<object>.Success(new { Success = true }, "Game state updated successfully", 200,
                "/api/game-sessions/state");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/state");
        }
    }
    public async Task<ApiResponse<object>> UpdateCurrentQuestionIndexAsync(int id, int questionIndex)
    {
        try
        {
            var success = await _gameSessionService.UpdateCurrentQuestionIndexAsync(id, questionIndex);
            if (!success)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND",
                    "/api/game-sessions/question-index");
            return ApiResponse<object>.Success(new { Success = true }, "Question index updated successfully", 200,
                "/api/game-sessions/question-index");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/question-index");
        }
    }
    public async Task<ApiResponse<object>> EndGameSessionAsync(int id)
    {
        try
        {
            var success = await _gameSessionService.EndGameSessionAsync(id);
            if (!success)
                return ApiResponse<object>.Fail("Game session not found", 404, "NOT_FOUND", "/api/game-sessions/end");
            return ApiResponse<object>.Success(new { Success = true }, "Game session ended successfully", 200,
                "/api/game-sessions/end");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/end");
        }
    }
    public async Task<ApiResponse<object>> GetGameQuestionsAsync(int gameSessionId)
    {
        try
        {
            var questions = await _gameSessionService.GetGameQuestionsAsync(gameSessionId);
            return ApiResponse<object>.Success(questions, "Game questions retrieved successfully", 200,
                "/api/game-sessions/questions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/questions");
        }
    }
    public async Task<ApiResponse<object>> AddQuestionsToGameSessionAsync(int gameSessionId, List<int> questionIds,
        int timeLimit)
    {
        try
        {
            var success =
                await _gameSessionService.AddQuestionsToGameSessionAsync(gameSessionId, questionIds, timeLimit);
            if (!success)
                return ApiResponse<object>.Fail("Failed to add questions to game session", 400, "BAD_REQUEST",
                    "/api/game-sessions/questions");
            return ApiResponse<object>.Success(new { Success = true }, "Questions added to game session successfully",
                200, "/api/game-sessions/questions");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR",
                "/api/game-sessions/questions");
        }
    }
}
