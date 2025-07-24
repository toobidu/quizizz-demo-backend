using ConsoleApp1.Config;
using ConsoleApp1.Model.DTO.Rooms;
using ConsoleApp1.Model.Entity.Questions;
using ConsoleApp1.Repository.Interface;

namespace ConsoleApp1.Controller;

public class RankingController
{
    private readonly IRankRepository _rankRepository;

    public RankingController(IRankRepository rankRepository)
    {
        _rankRepository = rankRepository;
    }

    public async Task<ApiResponse<object>> GetGlobalRanksAsync(int page, int limit, string level)
    {
        try
        {
            var ranks = await _rankRepository.GetGlobalRanksAsync(page, limit, level);
            var totalCount = await _rankRepository.GetTotalRanksCountAsync(level);
            
            var result = new
            {
                rankings = ranks,
                pagination = new
                {
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling((double)totalCount / limit),
                    totalUsers = totalCount,
                    hasNext = page * limit < totalCount,
                    hasPrevious = page > 1
                }
            };
            
            return ApiResponse<object>.Success(result, "Lấy bảng xếp hạng toàn cầu thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy bảng xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetLeaderboardAsync(int top)
    {
        try
        {
            var leaderboard = await _rankRepository.GetTopRanksAsync(top);
            return ApiResponse<object>.Success(leaderboard, "Lấy leaderboard thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy leaderboard: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetUserRankAsync(int userId)
    {
        try
        {
            var userRank = await _rankRepository.GetRankByUserIdAsync(userId);
            if (userRank == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy thông tin xếp hạng cho user này");
            }
            
            // Lấy thêm thông tin về vị trí xếp hạng toàn cầu
            var globalRank = await _rankRepository.GetUserGlobalRankPositionAsync(userId);
            
            var result = new
            {
                userId = userRank.UserId,
                globalRank,
                totalScore = userRank.TotalScore,
                gamesPlayed = userRank.GamesPlayed,
                rankLevel = await _rankRepository.CalculateUserLevelAsync(userRank.TotalScore)
            };
            
            return ApiResponse<object>.Success(result, "Lấy thông tin xếp hạng thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy thông tin xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> UpdateUserRankAsync(UpdateUserRankRequest request)
    {
        try
        {
            var existingRank = await _rankRepository.GetRankByUserIdAsync(request.UserId);
            if (existingRank == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy thông tin xếp hạng cho user này");
            }
            
            // Cập nhật thông tin xếp hạng
            existingRank.TotalScore += request.ScoreToAdd;
            existingRank.GamesPlayed += request.GamesPlayedToAdd;
            existingRank.UpdatedAt = DateTime.UtcNow;
            
            await _rankRepository.UpdateRankAsync(existingRank);
            
            // Tính toán level mới dựa trên tổng điểm
            var newLevel = await _rankRepository.CalculateUserLevelAsync(existingRank.TotalScore);
            
            var result = new
            {
                userId = existingRank.UserId,
                newTotalScore = existingRank.TotalScore,
                newGamesPlayed = existingRank.GamesPlayed,
                rankLevel = newLevel
            };
            
            return ApiResponse<object>.Success(result, "Cập nhật xếp hạng thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi cập nhật xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> InitializeUserRankAsync(InitializeUserRankRequest request)
    {
        try
        {
            var existingRank = await _rankRepository.GetRankByUserIdAsync(request.UserId);
            if (existingRank != null)
            {
                return ApiResponse<object>.Fail("User đã có thông tin xếp hạng");
            }
            
            var now = DateTime.UtcNow;
            var newRank = new Rank
            {
                UserId = request.UserId,
                TotalScore = request.InitialScore,
                GamesPlayed = 0,
                CreatedAt = now,
                UpdatedAt = now
            };
            
            await _rankRepository.CreateRankAsync(newRank);
            
            return ApiResponse<object>.Success(new
            {
                userId = newRank.UserId,
                totalScore = newRank.TotalScore,
                gamesPlayed = newRank.GamesPlayed,
                rankLevel = request.InitialLevel
            }, "Khởi tạo xếp hạng thành công", 201);
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi khởi tạo xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetUserRankHistoryAsync(int userId, int page, int limit)
    {
        try
        {
            var history = await _rankRepository.GetUserRankHistoryAsync(userId, page, limit);
            var totalCount = await _rankRepository.GetUserRankHistoryCountAsync(userId);
            
            var result = new
            {
                history,
                pagination = new
                {
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling((double)totalCount / limit),
                    totalEntries = totalCount,
                    hasNext = page * limit < totalCount,
                    hasPrevious = page > 1
                }
            };
            
            return ApiResponse<object>.Success(result, "Lấy lịch sử xếp hạng thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy lịch sử xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> GetRankingStatisticsAsync()
    {
        try
        {
            var totalUsers = await _rankRepository.GetTotalRanksCountAsync("all");
            var averageScore = await _rankRepository.GetAverageScoreAsync();
            var highestScore = await _rankRepository.GetHighestScoreAsync();
            
            var result = new
            {
                totalUsers,
                averageScore,
                highestScore,
                levelDistribution = await _rankRepository.GetLevelDistributionAsync()
            };
            
            return ApiResponse<object>.Success(result, "Lấy thống kê xếp hạng thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi lấy thống kê xếp hạng: " + ex.Message);
        }
    }

    public async Task<ApiResponse<object>> UpdateUserLevelAsync(int userId, UpdateUserLevelRequest request)
    {
        try
        {
            var existingRank = await _rankRepository.GetRankByUserIdAsync(userId);
            if (existingRank == null)
            {
                return ApiResponse<object>.Fail("Không tìm thấy thông tin xếp hạng cho user này");
            }
            
            // Cập nhật level thông qua điểm kinh nghiệm
            await _rankRepository.UpdateUserLevelAsync(userId, request.NewLevel, request.ExperiencePoints);
            
            return ApiResponse<object>.Success(new
            {
                userId,
                newLevel = request.NewLevel,
                experiencePoints = request.ExperiencePoints,
                reason = request.Reason
            }, "Cập nhật level thành công");
        }
        catch (Exception ex)
        {
            return ApiResponse<object>.Fail("Lỗi khi cập nhật level: " + ex.Message);
        }
    }
}