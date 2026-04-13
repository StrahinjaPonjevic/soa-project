using System.ComponentModel.DataAnnotations;

namespace BlogService.DTOs;

public class UpdateCommentDto
{
    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Text { get; set; }
}
