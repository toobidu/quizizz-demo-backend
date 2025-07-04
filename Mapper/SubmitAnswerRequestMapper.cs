using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

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
            timeTaken: TimeSpan.Zero
        );
    }
}