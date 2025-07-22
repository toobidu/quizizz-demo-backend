using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Questions;
namespace ConsoleApp1.Mapper.Questions;
public static class CorrectAnswerMapper
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
