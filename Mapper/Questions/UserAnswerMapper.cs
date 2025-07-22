using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Users;
namespace ConsoleApp1.Mapper.Questions;
public static class UserAnswerMapper
{
    public static UserAnswerDTO ToDTO(UserAnswer answer)
    {
        return new UserAnswerDTO(
            userId: answer.UserId,
            questionId: answer.QuestionId,
            selectedAnswerId: answer.AnswerId,
            isCorrect: answer.IsCorrect,
            timeTaken: answer.TimeTaken
        );
    }
    public static UserAnswer ToEntity(UserAnswerDTO dto)
    {
        return new UserAnswer(
            userId: dto.UserId,
            roomId: 0,
            questionId: dto.QuestionId,
            answerId: dto.SelectedAnswerId,
            isCorrect: dto.IsCorrect,
            timeTaken: dto.TimeTaken,
            createdAt: DateTime.UtcNow, // Default value
            updatedAt: DateTime.UtcNow
        );
    }
}
