namespace AuthService.Services
{
    public interface IStakeholdersClient
    {
        Task InitializeProfileAsync(int userId);
    }

}
