namespace ConsoleApp1.Model.Entity.Users;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; } 
    public string Address { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User() { }

    public User(string username, string fullName, string email, string phoneNumber, 
               string address, string password, string typeAccount, DateTime createdAt, DateTime updatedAt)
    {
        Username = username;
        FullName = fullName;
        Email = email;
        PhoneNumber = phoneNumber;
        Address = address;
        Password = password;
        TypeAccount = typeAccount;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}