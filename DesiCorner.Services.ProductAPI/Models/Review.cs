namespace DesiCorner.Services.ProductAPI.Models;

public class Review
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }

    // Rating from 1-5
    public int Rating { get; set; }

    // Review title (optional but recommended)
    public string? Title { get; set; }

    // Review content
    public string? Comment { get; set; }

    // For verified purchase badge
    public bool IsVerifiedPurchase { get; set; }

    // Moderation
    public bool IsApproved { get; set; } = true; // Auto-approve for now

    // Helpful votes
    public int HelpfulCount { get; set; } = 0;
    public int NotHelpfulCount { get; set; } = 0;

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Product Product { get; set; } = null!;

    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
}