using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Questions;

namespace ConsoleApp1.Mapper.Questions;

public static class QuestionMapper
{
    public static QuestionDTO ToDTO(Question question, List<AnswerDTO> options)
    {
        return new QuestionDTO(
            id: question.Id,
            questionText: question.QuestionText,
            options: options,
            topicId: question.TopicId ?? 0,
            questionTypeId: question.QuestionTypeId ?? 0,
            timeLimit: 30, // Default time limit
            points: 100 // Default points
        );
    }

    public static Question ToEntity(QuestionDTO questionDto)
    {
        return new Question(
            id: questionDto.Id,
            questionText: questionDto.QuestionText,
            topicId: questionDto.TopicId,
            questionTypeId: questionDto.QuestionTypeId,
            createdAt: DateTime.UtcNow,
            updatedAt: DateTime.UtcNow
        );
    }
}