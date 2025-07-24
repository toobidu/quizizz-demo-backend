using ConsoleApp1.Config;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Router;

namespace ConsoleApp1.Controller;

public class UserAnswerController
{
    private readonly IUserAnswerRepository _userAnswerRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IGameSessionRepository _gameSessionRepository;

    public UserAnswerController(
        IUserAnswerRepository userAnswerRepository,
        IQuestionRepository questionRepository,
        IGameSessionRepository gameSessionRepository)
    {
        _userAnswerRepository = userAnswerRepository;
        _questionRepository = questionRepository;
        _gameSessionRepository = gameSessionRepository;
    }

    public async Task<ApiResponse<object>> SubmitAnswerAsync(SubmitUserAnswerRequest request)
    {
        try
        {
            // Kiểm tra câu hỏi và câu trả lời có tồn tại không
            var question = await _questionRepository.GetQuestionByIdAsync(request.QuestionId);
            if (question == null)
            {
                return ApiResponse<object>.Fail("Câu hỏi không tồn tại");
            }

            // Kiểm tra session có tồn tại không
            var session = await _gameSessionRepository.GetGameSessionByIdAsync(request.SessionId);
            if (session == null)
            {
                return ApiResponse<object>.Fail("Phiên game không tồn tại");
            }

            // Kiểm tra câu trả lời có đúng không
            var isCorrect = await _questionRepository.CheckAnswerCorrectAsync(request.QuestionId, request.AnswerId);
            
            // Tính điểm dựa trên thời gian trả lời
            int score = CalculateScore(isCorrect, request.TimeToAnswer, session.TimeLimit);

            // Lưu câu trả lời
            var userAnswer = new UserAnswer
            {
                UserId = request.UserId,
                RoomId = session.RoomId,
                QuestionId = request.QuestionId,
                AnswerId = request.AnswerId,
                IsCorrect = isCorrect,
                TimeTaken = TimeSpan.FromSeconds(request.TimeToAnswer),
                GameSessionId = request.SessionId,
                Score = score,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userAnswerRepository.CreateUserAnswerAsync(userAnswer);

            return ApiResponse<object>.Success(new
            {
                userAnswerId = userAnswer.UserId, // Giả định ID được tạo
                userId = userAnswer.UserId,
                sessionId = userAnswer.GameSessionId,
                questionId = userAnswer.QuestionId,
                answerId = userAnswer.AnswerId,
                isCorrect = userAnswer.IsCorrect,
                timeToAnswer = request.TimeToAnswer,
                score = userAnswer.Score,
                submittedAt = userAnswer.CreatedAt
            }, "Đã ghi nhận câu trả lời", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi submit câu trả lời: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetAnswersBySessionAsync(int sessionId)
    {
        try
        {
            var answers = await _userAnswerRepository.GetAnswersBySessionIdAsync(sessionId);
            if (answers == null || !answers.Any())
            {
                return ApiResponse<object>.Success(new List<object>(), "Không có câu trả lời nào trong phiên này");
            }

            return ApiResponse<object>.Success(answers, "Lấy danh sách câu trả lời thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy câu trả lời theo phiên: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetAnswersByUserAndSessionAsync(int userId, int sessionId)
    {
        try
        {
            var answers = await _userAnswerRepository.GetAnswersByUserAndSessionAsync(userId, sessionId);
            if (answers == null || !answers.Any())
            {
                return ApiResponse<object>.Success(new List<object>(), "Không có câu trả lời nào của người dùng trong phiên này");
            }

            return ApiResponse<object>.Success(answers, "Lấy danh sách câu trả lời thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy câu trả lời theo người dùng và phiên: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetAnswersByUserAsync(int userId, int page, int limit)
    {
        try
        {
            var answers = await _userAnswerRepository.GetAnswersByUserIdAsync(userId, page, limit);
            var totalCount = await _userAnswerRepository.GetTotalAnswersCountByUserIdAsync(userId);

            var result = new
            {
                answers,
                pagination = new
                {
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling((double)totalCount / limit),
                    totalAnswers = totalCount,
                    hasNext = page * limit < totalCount,
                    hasPrevious = page > 1
                }
            };

            return ApiResponse<object>.Success(result, "Lấy danh sách câu trả lời thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy câu trả lời theo người dùng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> UpdateAnswerScoreAsync(int answerId, UpdateAnswerScoreRequest request)
    {
        try
        {
            var answer = await _userAnswerRepository.GetUserAnswerByIdAsync(answerId);
            if (answer == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy câu trả lời");
            }

            answer.Score = request.NewScore;
            answer.UpdatedAt = DateTime.UtcNow;

            await _userAnswerRepository.UpdateUserAnswerAsync(answer);

            return ApiResponse<object>.Success(new
            {
                answerId,
                newScore = request.NewScore,
                reason = request.Reason,
                updatedAt = answer.UpdatedAt
            }, "Cập nhật điểm thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi cập nhật điểm: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> DeleteAnswerAsync(int answerId)
    {
        try
        {
            var answer = await _userAnswerRepository.GetUserAnswerByIdAsync(answerId);
            if (answer == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy câu trả lời");
            }

            await _userAnswerRepository.DeleteUserAnswerAsync(answerId);

            return ApiResponse<object>.Success(new { answerId }, "Xóa câu trả lời thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi xóa câu trả lời: " + ex.Message);
        }
    }

    private int CalculateScore(bool isCorrect, double timeToAnswer, int timeLimit)
    {
        if (!isCorrect) return 0;

        // Tính điểm dựa trên thời gian trả lời
        // Công thức: Điểm cơ bản (100) * (1 + tỉ lệ thời gian còn lại)
        double timeRatio = Math.Max(0, 1 - (timeToAnswer / timeLimit));
        return (int)(100 * (1 + timeRatio));
    }
}