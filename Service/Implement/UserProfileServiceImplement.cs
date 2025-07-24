using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Model.Entity.Users;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
namespace ConsoleApp1.Service.Implement;
public class UserProfileServiceImplement : IUserProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserAnswerRepository _userAnswerRepository;
    private readonly IRankRepository _rankRepository;
    private readonly ITopicRepository _topicRepository;
    public UserProfileServiceImplement(
        IUserRepository userRepository,
        IUserAnswerRepository userAnswerRepository,
        IRankRepository rankRepository,
        ITopicRepository topicRepository)
    {
        _userRepository = userRepository;
        _userAnswerRepository = userAnswerRepository;
        _rankRepository = rankRepository;
        _topicRepository = topicRepository;
    }
    public async Task<UserProfileDTO?> GetUserProfileAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) return null;
        var stats = await GetUserStatsAsync(userId);
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, stats.HighestRank,
            stats.FastestTime, stats.HighestScore, stats.BestTopic, user.CreatedAt);
    }
    public async Task<UserProfileDTO?> SearchUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetUserByUsernameAsync(username);
        if (user == null) return null;
        var stats = await GetUserStatsAsync(user.Id);
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, stats.HighestRank,
            stats.FastestTime, stats.HighestScore, stats.BestTopic, user.CreatedAt);
    }
    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (!request.IsValid()) return false;
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user);
        return true;
    }
    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null) 
        {
            return false;
        }
        // Kiểm tra số điện thoại trùng lặp nếu có thay đổi
        if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
        {
            var existingUser = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
            if (existingUser != null && existingUser.Id != userId)
            {
                return false; // Số điện thoại đã tồn tại
            }
        }
        user.FullName = request.FullName ?? user.FullName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.Address = request.Address ?? user.Address;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user);
        return true;
    }
    private async Task<UserStats> GetUserStatsAsync(int userId)
    {
        // Lấy danh sách câu trả lời của người dùng
        var userAnswers = await _userAnswerRepository.GetByUserIdAsync(userId) ?? new List<UserAnswer>();
        var highestRank = await GetHighestRankAsync(userId);
        var fastestTime = userAnswers.Any() ? userAnswers.Min(ua => ua.TimeTaken) : TimeSpan.Zero;
        var highestScore = await GetHighestScoreAsync(userId, userAnswers);
        var bestTopic = await GetBestTopicAsync(userId, userAnswers);
        return new UserStats(highestRank, fastestTime, highestScore, bestTopic);
    }
    private async Task<int> GetHighestRankAsync(int userId)
    {
        var rank = await _rankRepository.GetRankByUserIdAsync(userId);
        return rank?.TotalScore ?? 0;
    }
    private async Task<int> GetHighestScoreAsync(int userId, IEnumerable<UserAnswer> userAnswers)
    {
        if (!userAnswers.Any()) return 0;
        // Tính điểm cao nhất dựa trên room session
        var roomSessions = userAnswers.GroupBy(ua => ua.RoomId)
            .Where(g => g.Key > 0)
            .Select(g => g.Count(ua => ua.IsCorrect) * 10) // 10 điểm mỗi câu đúng
            .DefaultIfEmpty(0);
        return roomSessions.Max();
    }
    private async Task<string> GetBestTopicAsync(int userId, IEnumerable<UserAnswer> userAnswers)
    {
        if (!userAnswers.Any()) return "None";
        var topicStats = userAnswers
            .Where(ua => ua.Question?.TopicId != null)
            .GroupBy(ua => ua.Question!.TopicId!.Value)
            .Select(g => new { 
                TopicId = g.Key, 
                CorrectRate = g.Count(ua => ua.IsCorrect) / (double)g.Count(),
                TotalAnswers = g.Count()
            })
            .Where(ts => ts.TotalAnswers >= 3) // Ít nhất 3 câu hỏi
            .OrderByDescending(ts => ts.CorrectRate)
            .ThenByDescending(ts => ts.TotalAnswers)
            .FirstOrDefault();
        if (topicStats != null)
        {
            var topic = await _topicRepository.GetByIdAsync(topicStats.TopicId);
            return topic?.Name ?? "Unknown";
        }
        return "None";
    }
    private record UserStats(int HighestRank, TimeSpan FastestTime, int HighestScore, string BestTopic);
}
