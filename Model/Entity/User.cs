namespace ConsoleApp1.Model.Entity;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string TypeAccount { get; set; }

    public User(string username, string password, string typeAccount) =>
        (Username, Password, TypeAccount) = (username, password, typeAccount);

    public User()
    {
    }
}