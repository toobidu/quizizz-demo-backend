using ConsoleApp1.Model.DTO.Game;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;

namespace ConsoleApp1.Service.Helper;

/// <summary>
/// Helper class để lấy câu hỏi cho game mà không cần thay đổi architecture hiện tại
/// </summary>
public static class GameQuestionHelper
{
    /// <summary>
    /// Lấy câu hỏi ngẫu nhiên theo topic từ room settings
    /// </summary>
    public static async Task<List<QuestionData>> GetQuestionsForRoomAsync(
        string roomCode, 
        IQuestionRepository questionRepository,
        IRoomSettingsRepository roomSettingsRepository,
        IRoomRepository roomRepository)
    {
        try
        {
            // 1. Lấy room từ database
            var room = await roomRepository.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                Console.WriteLine($"❌ [GameQuestionHelper] Room with code {roomCode} not found");
                return new List<QuestionData>();
            }

            // 2. Lấy settings của room
            var settings = await roomSettingsRepository.GetSettingsByRoomIdAsync(room.Id);
            var settingsDict = settings.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            var topicId = settingsDict.TryGetValue("topic_id", out var topicIdStr) ? int.Parse(topicIdStr) : 1;
            var questionCount = settingsDict.TryGetValue("question_count", out var questionCountStr) ? int.Parse(questionCountStr) : 10;

            Console.WriteLine($"🔧 [GameQuestionHelper] Room {roomCode} settings - TopicId: {topicId}, QuestionCount: {questionCount}");

            // 3. Lấy câu hỏi ngẫu nhiên theo topic
            var questions = await questionRepository.GetRandomQuestionsAsync(questionCount, topicId);
            var questionsList = questions.ToList();

            if (questionsList.Count == 0)
            {
                Console.WriteLine($"❌ [GameQuestionHelper] No questions found for topic {topicId}, trying without topic filter");
                // Fallback: lấy câu hỏi random không theo topic
                questions = await questionRepository.GetRandomQuestionsAsync(questionCount);
                questionsList = questions.ToList();
            }

            Console.WriteLine($"📝 [GameQuestionHelper] Found {questionsList.Count} questions for room {roomCode}");

            // 4. Chuyển đổi sang QuestionData format
            var questionDataList = questionsList.Select(q => new QuestionData
            {
                QuestionId = q.Id,
                Question = q.QuestionText ?? "",
                Options = new List<string>(), // Sẽ được load sau nếu cần
                CorrectAnswer = "",
                Type = "multiple_choice",
                Topic = topicId.ToString()
            }).ToList();

            return questionDataList;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameQuestionHelper] Error getting questions for room {roomCode}: {ex.Message}");
            return new List<QuestionData>();
        }
    }

    /// <summary>
    /// Tạo GameSession trong database
    /// </summary>
    public static async Task<int> CreateGameSessionInDatabaseAsync(
        string roomCode,
        List<Question> questions,
        IRoomRepository roomRepository,
        ConsoleApp1.Service.Interface.IGameSessionService gameSessionService)
    {
        try
        {
            var room = await roomRepository.GetRoomByCodeAsync(roomCode);
            if (room == null)
            {
                Console.WriteLine($"❌ [GameQuestionHelper] Room {roomCode} not found for GameSession creation");
                return 0;
            }

            // Tạo GameSession
            var gameSession = new ConsoleApp1.Model.Entity.Rooms.GameSession
            {
                RoomId = room.Id,
                GameState = "playing",
                CurrentQuestionIndex = 0,
                TimeLimit = 30 * questions.Count, // 30 giây mỗi câu
                StartTime = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var gameSessionId = await gameSessionService.CreateAsync(gameSession);

            // Thêm questions vào session
            var questionIds = questions.Select(q => q.Id).ToList();
            await gameSessionService.AddQuestionsToGameSessionAsync(gameSessionId, questionIds, 30);

            Console.WriteLine($"💾 [GameQuestionHelper] Created GameSession {gameSessionId} with {questionIds.Count} questions");

            return gameSessionId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [GameQuestionHelper] Error creating GameSession for room {roomCode}: {ex.Message}");
            return 0;
        }
    }
}
