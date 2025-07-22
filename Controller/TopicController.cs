using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Questions;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Mapper.Questions;
namespace ConsoleApp1.Controller;
public class TopicController
{
    private readonly ITopicRepository _topicRepository;
    public TopicController(ITopicRepository topicRepository)
    {
        _topicRepository = topicRepository;
    }
    public async Task<ApiResponse<IEnumerable<TopicDTO>>> GetAllTopicsAsync()
    {
        var topics = await _topicRepository.GetAllAsync();
        var topicDTOs = topics.Select(t => TopicMapper.ToDTO(t, 0));
        return ApiResponse<IEnumerable<TopicDTO>>.Success(topicDTOs, "Lấy danh sách chủ đề thành công");
    }
    public async Task<ApiResponse<TopicDTO>> GetTopicByIdAsync(int id)
    {
        var topic = await _topicRepository.GetByIdAsync(id);
        return topic != null 
            ? ApiResponse<TopicDTO>.Success(TopicMapper.ToDTO(topic, 0), "Lấy thông tin chủ đề thành công")
            : ApiResponse<TopicDTO>.Fail("Không tìm thấy chủ đề");
    }
}
