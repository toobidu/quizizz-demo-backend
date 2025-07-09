using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Mapper.Questions;

public static class AnswerMapper
{
    public static AnswerDTO ToDTO(Answer answer)
    {
        return new AnswerDTO(
            id: answer.Id,
            answerText: answer.AnswerText,
            optionIndex: 0
        );
    }

    public static Answer ToEntity(AnswerDTO answerDto)
    {
        return new Answer(
            id: answerDto.Id,
            questionId: 0,
            answerText: answerDto.AnswerText,
            isCorrect: false,
            createdAt: DateTime.UtcNow, 
            updatedAt: DateTime.UtcNow  
        );
    }
}