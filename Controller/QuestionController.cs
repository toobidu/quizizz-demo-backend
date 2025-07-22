using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Mapper.Questions;

namespace ConsoleApp1.Controller;

public class QuestionController
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IAnswerRepository _answerRepository;

    public QuestionController(IQuestionRepository questionRepository, IRoomRepository roomRepository, IAnswerRepository answerRepository)
    {
        _questionRepository = questionRepository;
        _roomRepository = roomRepository;
        _answerRepository = answerRepository;
    }

    /// <summary>
    /// Lấy danh sách câu hỏi cho room theo roomCode
    /// </summary>
    public async Task<ApiResponse<IEnumerable<QuestionDTO>>> GetQuestionsForRoomAsync(string roomCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(roomCode))
            {
                return ApiResponse<IEnumerable<QuestionDTO>>.Fail("Room code is required", 400, "INVALID_ROOM_CODE");
            }

            // Kiểm tra room có tồn tại không
            var room = await _roomRepository.GetByCodeAsync(roomCode);
            if (room == null)
            {
                return ApiResponse<IEnumerable<QuestionDTO>>.Fail("Room not found", 404, "ROOM_NOT_FOUND");
            }

            // Lấy tất cả questions (có thể customize logic sau)
            var questions = await _questionRepository.GetAllAsync();
            
            if (!questions.Any())
            {
                return ApiResponse<IEnumerable<QuestionDTO>>.Success(
                    Enumerable.Empty<QuestionDTO>(), 
                    "No questions found", 
                    200, 
                    $"/api/game/questions/{roomCode}"
                );
            }

            // Convert to DTO with answers
            var questionDTOs = new List<QuestionDTO>();
            foreach (var question in questions)
            {
                // Lấy answers cho từng question
                var answers = await _answerRepository.GetByQuestionIdAsync(question.Id);
                var answerDTOs = answers.Select(a => AnswerMapper.ToDTO(a)).ToList();
                
                var questionDTO = QuestionMapper.ToDTO(question, answerDTOs);
                questionDTOs.Add(questionDTO);
            }

            return ApiResponse<IEnumerable<QuestionDTO>>.Success(
                questionDTOs, 
                "Questions retrieved successfully", 
                200, 
                $"/api/game/questions/{roomCode}"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<QuestionDTO>>.Fail(
                "Server error: " + ex.Message, 
                500, 
                "SERVER_ERROR", 
                $"/api/game/questions/{roomCode}"
            );
        }
    }

    /// <summary>
    /// Lấy câu hỏi ngẫu nhiên theo topic
    /// </summary>
    public async Task<ApiResponse<IEnumerable<QuestionDTO>>> GetRandomQuestionsAsync(int count, int? topicId = null)
    {
        try
        {
            var questions = await _questionRepository.GetRandomQuestionsAsync(count, topicId);
            var questionDTOs = new List<QuestionDTO>();
            foreach (var question in questions)
            {
                var answers = await _answerRepository.GetByQuestionIdAsync(question.Id);
                var answerDTOs = answers.Select(a => AnswerMapper.ToDTO(a)).ToList();
                var questionDTO = QuestionMapper.ToDTO(question, answerDTOs);
                questionDTOs.Add(questionDTO);
            }

            return ApiResponse<IEnumerable<QuestionDTO>>.Success(
                questionDTOs, 
                $"Retrieved {questionDTOs.Count} random questions", 
                200
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<QuestionDTO>>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR");
        }
    }

    /// <summary>
    /// Lấy câu hỏi theo topic
    /// </summary>
    public async Task<ApiResponse<IEnumerable<QuestionDTO>>> GetQuestionsByTopicAsync(int topicId)
    {
        try
        {
            var questions = await _questionRepository.GetByTopicIdAsync(topicId);
            var questionDTOs = new List<QuestionDTO>();
            foreach (var question in questions)
            {
                var answers = await _answerRepository.GetByQuestionIdAsync(question.Id);
                var answerDTOs = answers.Select(a => AnswerMapper.ToDTO(a)).ToList();
                var questionDTO = QuestionMapper.ToDTO(question, answerDTOs);
                questionDTOs.Add(questionDTO);
            }

            return ApiResponse<IEnumerable<QuestionDTO>>.Success(
                questionDTOs, 
                $"Retrieved {questionDTOs.Count} questions for topic {topicId}", 
                200
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<QuestionDTO>>.Fail("Server error: " + ex.Message, 500, "SERVER_ERROR");
        }
    }
}
