namespace ConsoleApp1.Model.DTO.Users;

public class UpdateProfileRequest
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }

    public bool ValidField()
    {
        return !string.IsNullOrWhiteSpace(FullName) &&
               !string.IsNullOrWhiteSpace(Email);
    }
}
