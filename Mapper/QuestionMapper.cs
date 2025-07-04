using ConsoleApp1.Model.DTO;
using ConsoleApp1.Model.Entity;

namespace ConsoleApp1.Mapper;

public static class QuestionMapper
{
    public static QuestionDTO ToDTO(Question question, List<AnswerDTO> options)
    {
        return new QuestionDTO(
            id: question.Id,
            questionText: question.QuestionText,
            options: options
        );
    }

    public static Question ToEntity(QuestionDTO questionDto)
    {
        return new Question(
            id: questionDto.Id,
            questionText: questionDto.QuestionText
        );
    }
}