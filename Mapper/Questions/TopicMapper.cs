using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Model.Entity.Questions;
namespace ConsoleApp1.Mapper.Questions;
public static class TopicMapper
{
    public static TopicDTO ToDTO(Topic topic, int questionCount)
    {
        return new TopicDTO(
            id: topic.Id,
            name: topic.Name,
            questionCount: questionCount
        );
    }
    public static Topic ToEntity(TopicDTO topicDto)
    {
        return new Topic(
            id: topicDto.Id,
            name: topicDto.Name
        );
    }
}
