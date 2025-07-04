namespace ConsoleApp1.Model.Entity;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }
    
    public User(int id, string username, string password, string typeAccount) =>
        (Id, Username, Password, TypeAccount) = (id, username, password, typeAccount);
}