using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;
using ConsoleApp1.Service.Interface;
using BCrypt.Net;

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
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return null;

        var stats = await GetUserStatsAsync(userId);
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, stats.HighestRank,
            stats.FastestTime, stats.HighestScore, stats.BestTopic);
    }

    public async Task<UserProfileDTO?> SearchUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null) return null;

        var stats = await GetUserStatsAsync(user.Id);
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, stats.HighestRank,
            stats.FastestTime, stats.HighestScore, stats.BestTopic);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (!request.IsValid()) return false;

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password))
            return false;

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Address = request.Address;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return true;
    }

    private async Task<UserStats> GetUserStatsAsync(int userId)
    {
        var userAnswers = await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 5);
        
        var highestRank = await GetHighestRankAsync(userId);
        var fastestTime = userAnswers.Any() ? userAnswers.Min(ua => ua.TimeTaken) : TimeSpan.Zero;
        var highestScore = await GetHighestScoreAsync(userId);
        var bestTopic = await GetBestTopicAsync(userId);

        return new UserStats(highestRank, fastestTime, highestScore, bestTopic);
    }

    private async Task<int> GetHighestRankAsync(int userId)
    {
        var rank = await _rankRepository.GetByUserIdAsync(userId);
        return rank?.TotalScore ?? 0;
    }

    private async Task<int> GetHighestScoreAsync(int userId)
    {
        var userAnswers = await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 5);
        return userAnswers.Any() ? userAnswers.Max(ua => ua.IsCorrect ? 100 : 0) : 0;
    }

    private async Task<string> GetBestTopicAsync(int userId)
    {
        var userAnswers = await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 5);
        if (!userAnswers.Any()) return "None";

        var topicStats = userAnswers
            .Where(ua => ua.Question?.TopicId != null)
            .GroupBy(ua => ua.Question.TopicId.Value)
            .Select(g => new { TopicId = g.Key, CorrectCount = g.Count(ua => ua.IsCorrect) })
            .OrderByDescending(ts => ts.CorrectCount)
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