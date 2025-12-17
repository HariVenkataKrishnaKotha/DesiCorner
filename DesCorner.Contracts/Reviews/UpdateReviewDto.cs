using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Reviews;

public class UpdateReviewDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
}