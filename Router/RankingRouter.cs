using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;
using ConsoleApp1.Config;
using ConsoleApp1.Controller;

namespace ConsoleApp1.Router;

/// <summary>
/// Router xử lý các API liên quan đến Rankings và Leaderboard
/// Quản lý hệ thống xếp hạng người chơi toàn cầu
/// </summary>
public class RankingRouter : IBaseRouter
{
    // TODO: Cần tạo RankingController khi implement
    // private readonly RankingController _rankingController;

    public RankingRouter()
    {
        // TODO: Inject RankingController khi được tạo
        // _rankingController = rankingController;
    }

    public async Task<bool> HandleAsync(HttpListenerRequest request, HttpListenerResponse response)
    {
        string path = request.Url?.AbsolutePath ?? "";
        string method = request.HttpMethod;

        // Chỉ xử lý các request đến /api/ranks
        if (!path.StartsWith("/api/ranks"))
            return false;

        // Handle CORS preflight
        if (method.ToUpper() == "OPTIONS")
        {
            HttpResponseHelper.WriteOptionsResponse(response);
            return true;
        }

        try
        {
            switch ((method.ToUpper(), path))
            {
                // GET /api/ranks/global - Bảng xếp hạng toàn cầu
                case ("GET", "/api/ranks/global"):
                    await HandleGetGlobalRanks(request, response);
                    return true;

                // GET /api/ranks/leaderboard - Top players leaderboard
                case ("GET", "/api/ranks/leaderboard"):
                    await HandleGetLeaderboard(request, response);
                    return true;

                // GET /api/ranks/user/{userId} - Xếp hạng của user cụ thể
                case var (m, p) when m == "GET" && p.StartsWith("/api/ranks/user/"):
                    await HandleGetUserRank(request, response, path);
                    return true;

                // POST /api/ranks/update - Cập nhật điểm cho user
                case ("POST", "/api/ranks/update"):
                    await HandleUpdateUserRank(request, response);
                    return true;

                // POST /api/ranks/initialize - Khởi tạo rank cho user mới
                case ("POST", "/api/ranks/initialize"):
                    await HandleInitializeUserRank(request, response);
                    return true;

                // GET /api/ranks/history/{userId} - Lịch sử điểm của user
                case var (m, p) when m == "GET" && p.StartsWith("/api/ranks/history/"):
                    await HandleGetUserRankHistory(request, response, path);
                    return true;

                // GET /api/ranks/statistics - Thống kê rankings
                case ("GET", "/api/ranks/statistics"):
                    await HandleGetRankingStatistics(request, response);
                    return true;

                // PUT /api/ranks/user/{userId}/level - Cập nhật level của user
                case var (m, p) when m == "PUT" && p.Contains("/level"):
                    await HandleUpdateUserLevel(request, response, path);
                    return true;

                default:
                    HttpResponseHelper.WriteNotFound(response, "Ranking API endpoint không tồn tại", path);
                    return true;
            }
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi server khi xử lý Ranking API: " + ex.Message, path);
            return true;
        }
    }

    #region Handler Methods

    /// <summary>
    /// Lấy bảng xếp hạng toàn cầu
    /// GET /api/ranks/global?page=1&limit=50&level=all
    /// </summary>
    private async Task HandleGetGlobalRanks(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            int page = int.TryParse(queryParams["page"], out var p) ? p : 1;
            int limit = int.TryParse(queryParams["limit"], out var l) ? l : 50;
            string level = queryParams["level"] ?? "all";

            // Validate pagination
            if (page < 1) page = 1;
            if (limit < 1 || limit > 100) limit = 50;

            // TODO: Implement RankingController.GetGlobalRanksAsync(page, limit, level)
            // var result = await _rankingController.GetGlobalRanksAsync(page, limit, level);

            // Temporary response
            var tempResponse = new
            {
                rankings = new[]
                {
                    new
                    {
                        rank = 1,
                        userId = 1,
                        username = "champion_player",
                        totalScore = 5000,
                        gamesPlayed = 50,
                        gamesWon = 40,
                        winRate = 0.8,
                        rankLevel = "Diamond",
                        averageScore = 100.0,
                        lastActive = DateTime.UtcNow.AddDays(-1)
                    },
                    new
                    {
                        rank = 2,
                        userId = 2,
                        username = "pro_gamer",
                        totalScore = 4500,
                        gamesPlayed = 45,
                        gamesWon = 32,
                        winRate = 0.71,
                        rankLevel = "Gold",
                        averageScore = 95.5,
                        lastActive = DateTime.UtcNow.AddHours(-2)
                    }
                },
                pagination = new
                {
                    page,
                    limit,
                    totalPages = 10,
                    totalUsers = 500,
                    hasNext = page < 10,
                    hasPrevious = page > 1
                },
                message = "Global rankings retrieved successfully (TEMPORARY IMPLEMENTATION)"
            };

            HttpResponseHelper.WriteJsonResponse(response, 
                ApiResponse<object>.Success(tempResponse, "Lấy bảng xếp hạng toàn cầu thành công"));
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy global rankings: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Lấy leaderboard top players
    /// GET /api/ranks/leaderboard?top=10
    /// </summary>
    private async Task HandleGetLeaderboard(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            int top = int.TryParse(queryParams["top"], out var t) ? t : 10;
            
            if (top < 1 || top > 100) top = 10;

            // TODO: Implement RankingController.GetLeaderboardAsync(top)
            // var result = await _rankingController.GetLeaderboardAsync(top);

            HttpResponseHelper.WriteNotFound(response, "Leaderboard API chưa được implement", request.Url?.AbsolutePath);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy leaderboard: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Lấy xếp hạng của user cụ thể
    /// GET /api/ranks/user/{userId}
    /// </summary>
    private async Task HandleGetUserRank(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int userId))
            {
                HttpResponseHelper.WriteBadRequest(response, "User ID không hợp lệ", path);
                return;
            }

            // TODO: Implement RankingController.GetUserRankAsync(userId)
            // var result = await _rankingController.GetUserRankAsync(userId);

            // Temporary response
            var tempResponse = new
            {
                userId,
                username = "test_user",
                globalRank = 25,
                totalScore = 2500,
                gamesPlayed = 30,
                gamesWon = 20,
                winRate = 0.67,
                rankLevel = "Silver",
                averageScore = 83.3,
                experiencePoints = 1500,
                nextLevelPoints = 2000,
                progressToNextLevel = 0.75,
                recentGames = new[]
                {
                    new { gameId = 1, score = 95, date = DateTime.UtcNow.AddDays(-1) },
                    new { gameId = 2, score = 88, date = DateTime.UtcNow.AddDays(-2) }
                },
                message = "User rank retrieved successfully (TEMPORARY IMPLEMENTATION)"
            };

            HttpResponseHelper.WriteJsonResponse(response, 
                ApiResponse<object>.Success(tempResponse, "Lấy xếp hạng user thành công"));
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy user rank: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Cập nhật điểm cho user
    /// POST /api/ranks/update
    /// </summary>
    private async Task HandleUpdateUserRank(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream);
            var json = await reader.ReadToEndAsync();
            var updateRequest = JsonSerializer.Deserialize<UpdateUserRankRequest>(json, JsonSerializerConfig.DefaultOptions);

            if (updateRequest == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu update không hợp lệ", request.Url?.AbsolutePath);
                return;
            }

            // Validate required fields
            if (updateRequest.UserId <= 0 || updateRequest.ScoreToAdd < 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "UserId và ScoreToAdd là bắt buộc", request.Url?.AbsolutePath);
                return;
            }

            // TODO: Implement RankingController.UpdateUserRankAsync(updateRequest)
            // var result = await _rankingController.UpdateUserRankAsync(updateRequest);

            HttpResponseHelper.WriteNotFound(response, "Update User Rank API chưa được implement", request.Url?.AbsolutePath);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi cập nhật user rank: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Khởi tạo rank cho user mới
    /// POST /api/ranks/initialize
    /// </summary>
    private async Task HandleInitializeUserRank(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            using var reader = new StreamReader(request.InputStream);
            var json = await reader.ReadToEndAsync();
            var initRequest = JsonSerializer.Deserialize<InitializeUserRankRequest>(json, JsonSerializerConfig.DefaultOptions);

            if (initRequest == null || initRequest.UserId <= 0)
            {
                HttpResponseHelper.WriteBadRequest(response, "UserId là bắt buộc", request.Url?.AbsolutePath);
                return;
            }

            // TODO: Implement RankingController.InitializeUserRankAsync(initRequest)
            // var result = await _rankingController.InitializeUserRankAsync(initRequest);

            HttpResponseHelper.WriteNotFound(response, "Initialize User Rank API chưa được implement", request.Url?.AbsolutePath);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi khởi tạo user rank: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Lấy lịch sử điểm của user
    /// GET /api/ranks/history/{userId}?page=1&limit=20
    /// </summary>
    private async Task HandleGetUserRankHistory(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int userId))
            {
                HttpResponseHelper.WriteBadRequest(response, "User ID không hợp lệ", path);
                return;
            }

            var queryParams = HttpUtility.ParseQueryString(request.Url?.Query ?? "");
            int page = int.TryParse(queryParams["page"], out var p) ? p : 1;
            int limit = int.TryParse(queryParams["limit"], out var l) ? l : 20;

            // TODO: Implement RankingController.GetUserRankHistoryAsync(userId, page, limit)
            // var result = await _rankingController.GetUserRankHistoryAsync(userId, page, limit);

            HttpResponseHelper.WriteNotFound(response, "Get User Rank History API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy lịch sử rank: " + ex.Message, path);
        }
    }

    /// <summary>
    /// Lấy thống kê rankings
    /// GET /api/ranks/statistics
    /// </summary>
    private async Task HandleGetRankingStatistics(HttpListenerRequest request, HttpListenerResponse response)
    {
        try
        {
            // TODO: Implement RankingController.GetRankingStatisticsAsync()
            // var result = await _rankingController.GetRankingStatisticsAsync();

            HttpResponseHelper.WriteNotFound(response, "Ranking Statistics API chưa được implement", request.Url?.AbsolutePath);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi lấy thống kê rankings: " + ex.Message, request.Url?.AbsolutePath);
        }
    }

    /// <summary>
    /// Cập nhật level của user
    /// PUT /api/ranks/user/{userId}/level
    /// </summary>
    private async Task HandleUpdateUserLevel(HttpListenerRequest request, HttpListenerResponse response, string path)
    {
        try
        {
            var pathParts = path.Split('/');
            if (pathParts.Length < 4 || !int.TryParse(pathParts[3], out int userId))
            {
                HttpResponseHelper.WriteBadRequest(response, "User ID không hợp lệ", path);
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var json = await reader.ReadToEndAsync();
            var updateRequest = JsonSerializer.Deserialize<UpdateUserLevelRequest>(json, JsonSerializerConfig.DefaultOptions);

            if (updateRequest == null)
            {
                HttpResponseHelper.WriteBadRequest(response, "Dữ liệu update level không hợp lệ", path);
                return;
            }

            // TODO: Implement RankingController.UpdateUserLevelAsync(userId, updateRequest)
            // var result = await _rankingController.UpdateUserLevelAsync(userId, updateRequest);

            HttpResponseHelper.WriteNotFound(response, "Update User Level API chưa được implement", path);
        }
        catch (Exception ex)
        {
            HttpResponseHelper.WriteInternalServerError(response, 
                "Lỗi khi cập nhật user level: " + ex.Message, path);
        }
    }

    #endregion
}

#region Request Models

/// <summary>
/// Request model để update user rank
/// </summary>
public class UpdateUserRankRequest
{
    public int UserId { get; set; }
    public int ScoreToAdd { get; set; }
    public int GamesPlayedToAdd { get; set; } = 1;
    public bool GameWon { get; set; } = false;
    public string? GameType { get; set; }
}

/// <summary>
/// Request model để khởi tạo rank cho user mới
/// </summary>
public class InitializeUserRankRequest
{
    public int UserId { get; set; }
    public string InitialLevel { get; set; } = "Bronze";
    public int InitialScore { get; set; } = 0;
}

/// <summary>
/// Request model để update user level
/// </summary>
public class UpdateUserLevelRequest
{
    public string NewLevel { get; set; } = "";
    public int ExperiencePoints { get; set; }
    public string? Reason { get; set; }
}

#endregion
