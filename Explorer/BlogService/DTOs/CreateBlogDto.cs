using System.ComponentModel.DataAnnotations;

namespace BlogService.DTOs;

public class CreateBlogDto
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public required string Title { get; set; }

    [Required]
    [MinLength(1)]
    public required string DescriptionMarkdown { get; set; }

    public string[]? ImageUrls { get; set; }
}
