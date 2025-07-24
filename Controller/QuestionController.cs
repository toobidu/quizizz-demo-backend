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

    /// <summary>
    /// Lấy danh sách câu hỏi kèm theo câu trả lời theo tên chủ đề
    /// API này trả về câu hỏi đã được nhóm với danh sách câu trả lời của từng câu hỏi
    /// </summary>
    /// <param name="topicName">Tên chủ đề (ví dụ: "Toán học", "Lịch sử")</param>
    /// <returns>Danh sách câu hỏi kèm theo câu trả lời đã được nhóm</returns>
    public async Task<ApiResponse<IEnumerable<QuestionWithAnswersDTO>>> GetQuestionsWithAnswersByTopicNameAsync(string topicName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                return ApiResponse<IEnumerable<QuestionWithAnswersDTO>>.Fail(
                    "Tên chủ đề không được để trống", 
                    400, 
                    "INVALID_TOPIC_NAME"
                );
            }

            // Lấy raw data từ repository (JOIN query)
            var rawData = await _questionRepository.GetQuestionsWithAnswersByTopicNameAsync(topicName);
            
            if (!rawData.Any())
            {
                return ApiResponse<IEnumerable<QuestionWithAnswersDTO>>.Fail(
                    $"Không tìm thấy câu hỏi nào cho chủ đề '{topicName}'", 
                    404, 
                    "NO_QUESTIONS_FOUND"
                );
            }

            // Nhóm raw data thành câu hỏi với danh sách câu trả lời
            var groupedData = rawData
                .GroupBy(r => new { r.QuestionId, r.QuestionText, r.TopicName })
                .Select(g => new QuestionWithAnswersDTO
                {
                    TopicName = g.Key.TopicName,
                    QuestionId = g.Key.QuestionId,
                    QuestionText = g.Key.QuestionText,
                    Answers = g.Select(r => new AnswerDetailDTO
                    {
                        AnswerId = r.AnswerId,
                        AnswerText = r.AnswerText,
                        IsCorrect = r.IsCorrect
                    }).ToList()
                })
                .ToList();

            return ApiResponse<IEnumerable<QuestionWithAnswersDTO>>.Success(
                groupedData,
                $"Lấy thành công {groupedData.Count} câu hỏi cho chủ đề '{topicName}'",
                200
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<QuestionWithAnswersDTO>>.Fail(
                "Lỗi server: " + ex.Message, 
                500, 
                "SERVER_ERROR"
            );
        }
    }
    
    /// <summary>
    /// Lấy danh sách câu hỏi theo tên chủ đề (không kèm câu trả lời)
    /// </summary>
    /// <param name="topicName">Tên chủ đề (ví dụ: "Toán học", "Lịch sử")</param>
    /// <returns>Danh sách câu hỏi không kèm câu trả lời</returns>
    public async Task<ApiResponse<IEnumerable<QuestionDTO>>> GetQuestionsByTopicNameAsync(string topicName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(topicName))
            {
                return ApiResponse<IEnumerable<QuestionDTO>>.Fail(
                    "Tên chủ đề không được để trống", 
                    400, 
                    "INVALID_TOPIC_NAME"
                );
            }

            // Lấy raw data từ repository
            var rawData = await _questionRepository.GetQuestionsWithAnswersByTopicNameAsync(topicName);
            
            if (!rawData.Any())
            {
                return ApiResponse<IEnumerable<QuestionDTO>>.Fail(
                    $"Không tìm thấy câu hỏi nào cho chủ đề '{topicName}'", 
                    404, 
                    "NO_QUESTIONS_FOUND"
                );
            }

            // Chỉ lấy thông tin câu hỏi, không lấy câu trả lời
            var questions = rawData
                .GroupBy(r => new { r.QuestionId, r.QuestionText, r.TopicName })
                .Select(g => new QuestionDTO(
                    id: g.Key.QuestionId,
                    questionText: g.Key.QuestionText,
                    options: new List<AnswerDTO>(),
                    topicId: 0, // Default value since we don't have topicId in the raw data
                    questionTypeId: 0, // Default value
                    timeLimit: 30, // Default time limit
                    points: 100 // Default points
                ))
                .ToList();

            return ApiResponse<IEnumerable<QuestionDTO>>.Success(
                questions,
                $"Lấy thành công {questions.Count} câu hỏi cho chủ đề '{topicName}'",
                200
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<QuestionDTO>>.Fail(
                "Lỗi server: " + ex.Message, 
                500, 
                "SERVER_ERROR"
            );
        }
    }
}
