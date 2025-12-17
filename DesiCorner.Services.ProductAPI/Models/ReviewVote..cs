namespace DesiCorner.Services.ProductAPI.Models;

/// <summary>
/// Tracks individual user votes on reviews to prevent duplicate voting
/// </summary>
public class ReviewVote
{
    public Guid Id { get; set; }
    public Guid ReviewId { get; set; }
    public Guid UserId { get; set; }
    public bool IsHelpful { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Review Review { get; set; } = null!;
}