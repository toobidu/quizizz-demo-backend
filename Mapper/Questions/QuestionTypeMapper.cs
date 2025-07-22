using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Questions;
namespace ConsoleApp1.Mapper.Questions;
public static class QuestionTypeMapper
{
    public static QuestionTypeDTO ToDTO(QuestionType questionType)
    {
        return new QuestionTypeDTO(
            id: questionType.Id,
            name: questionType.Name
        );
    }
    public static QuestionType ToEntity(QuestionTypeDTO dto)
    {
        return new QuestionType(
            id: dto.Id,
            name: dto.Name
        );
    }
}
