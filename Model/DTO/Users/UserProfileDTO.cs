namespace ConsoleApp1.Model.DTO.Users;

public class UserProfileDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
    public int HighestRank { get; set; }
    public TimeSpan FastestTime { get; set; }
    public int HighestScore { get; set; }
    public string BestTopic { get; set; }

    public UserProfileDTO(int id, string username, string fullName, string email, 
                         string phoneNumber, string address, int highestRank, 
                         TimeSpan fastestTime, int highestScore, string bestTopic) =>
        (Id, Username, FullName, Email, PhoneNumber, Address, HighestRank, 
         FastestTime, HighestScore, BestTopic) = 
        (id, username, fullName, email, phoneNumber, address, highestRank, 
         fastestTime, highestScore, bestTopic);
}