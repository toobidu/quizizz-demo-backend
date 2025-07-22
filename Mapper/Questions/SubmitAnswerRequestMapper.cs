using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Users;
namespace ConsoleApp1.Mapper.Questions;
public static class SubmitAnswerRequestMapper
{
    public static UserAnswer ToEntity(SubmitAnswerRequest request, int userId, int roomId)
    {
        return new UserAnswer(
            userId: userId,
            roomId: roomId,
            questionId: request.QuestionId,
            answerId: request.AnswerId,
            isCorrect: false,
            timeTaken: TimeSpan.Zero,
            createdAt: DateTime.UtcNow, 
            updatedAt: DateTime.UtcNow 
        );
    }
}
