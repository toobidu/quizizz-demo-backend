using ConsoleApp1.Model.DTO.Users;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Model.Entity.Users;
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

        Console.WriteLine($"[DEBUG] User from DB - Id: {user.Id}, Username: {user.Username}, FullName: '{user.FullName}', PhoneNumber: '{user.PhoneNumber}', Address: '{user.Address}'");

        var stats = await GetUserStatsAsync(userId);
        return new UserProfileDTO(
            user.Id, user.Username, user.FullName, user.Email,
            user.PhoneNumber, user.Address, stats.HighestRank,
            stats.FastestTime, stats.HighestScore, stats.BestTopic, user.CreatedAt);
    }

    public async Task<UserProfileDTO?> SearchUserByUsernameAsync(string username)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
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
        Console.WriteLine($"[DEBUG] UpdateProfile - UserId: {userId}, FullName: '{request.FullName}', PhoneNumber: '{request.PhoneNumber}', Address: '{request.Address}'");
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) 
        {
            Console.WriteLine($"[DEBUG] User not found with ID: {userId}");
            return false;
        }

        Console.WriteLine($"[DEBUG] Current user - FullName: '{user.FullName}', PhoneNumber: '{user.PhoneNumber}', Address: '{user.Address}'");

        // Kiểm tra phone number trùng lặp nếu có thay đổi
        if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber)
        {
            Console.WriteLine($"[DEBUG] Checking phone number duplicate: {request.PhoneNumber}");
            var existingUser = await _userRepository.GetByPhoneNumberAsync(request.PhoneNumber);
            if (existingUser != null && existingUser.Id != userId)
            {
                Console.WriteLine($"[DEBUG] Phone number already exists for user ID: {existingUser.Id}");
                return false; // Phone number đã tồn tại
            }
        }

        user.FullName = request.FullName ?? user.FullName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.Address = request.Address ?? user.Address;
        user.UpdatedAt = DateTime.UtcNow;
        
        Console.WriteLine($"[DEBUG] Updating user - FullName: '{user.FullName}', PhoneNumber: '{user.PhoneNumber}', Address: '{user.Address}'");
        await _userRepository.UpdateAsync(user);
        Console.WriteLine($"[DEBUG] Update completed successfully");
        return true;
    }

    private async Task<UserStats> GetUserStatsAsync(int userId)
    {
        var userAnswers = await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 100); // Lấy nhiều hơn để tính toán chính xác
        
        var highestRank = await GetHighestRankAsync(userId);
        var fastestTime = userAnswers.Any() ? userAnswers.Min(ua => ua.TimeTaken) : TimeSpan.Zero;
        var highestScore = await GetHighestScoreAsync(userId, userAnswers);
        var bestTopic = await GetBestTopicAsync(userId, userAnswers);

        return new UserStats(highestRank, fastestTime, highestScore, bestTopic);
    }

    private async Task<int> GetHighestRankAsync(int userId)
    {
        var rank = await _rankRepository.GetByUserIdAsync(userId);
        return rank?.TotalScore ?? 0;
    }

    private async Task<int> GetHighestScoreAsync(int userId, IEnumerable<UserAnswer>? userAnswers = null)
    {
        userAnswers ??= await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 100);
        if (!userAnswers.Any()) return 0;
        
        // Tính điểm cao nhất dựa trên room session
        var roomSessions = userAnswers.GroupBy(ua => ua.RoomId)
            .Where(g => g.Key > 0)
            .Select(g => g.Count(ua => ua.IsCorrect) * 10) // 10 điểm mỗi câu đúng
            .DefaultIfEmpty(0);
            
        return roomSessions.Max();
    }

    private async Task<string> GetBestTopicAsync(int userId, IEnumerable<UserAnswer>? userAnswers = null)
    {
        userAnswers ??= await _userAnswerRepository.GetRecentAnswersByUserIdAsync(userId, 100);
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