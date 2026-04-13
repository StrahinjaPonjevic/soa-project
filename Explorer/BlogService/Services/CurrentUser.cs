namespace BlogService.Services;

public sealed class CurrentUser
{
    public CurrentUser(int userId, string username)
    {
        UserId = userId;
        Username = username;
    }

    public int UserId { get; }
    public string Username { get; }
}
