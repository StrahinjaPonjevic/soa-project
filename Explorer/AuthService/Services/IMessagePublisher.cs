namespace AuthService.Services
{
    public interface IMessagePublisher
    {
        Task PublishUserRegisteredAsync(int userId);
    }

}
