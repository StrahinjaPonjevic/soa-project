namespace BlogService.Services;

public interface ILikeService
{
    Task AddLikeAsync(int blogId, CurrentUser currentUser);
    Task RemoveLikeAsync(int blogId, CurrentUser currentUser);
}
