using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public class CorrectAnswerMapper
{
    public static CorrectAnswerDTO ToDTO(int questionId, Answer answer)
    {
        return new CorrectAnswerDTO(
            questionId: questionId,
            answerId: answer.Id,
            answerText: answer.AnswerText,
            isCorrect: answer.IsCorrect
        );
    }
}