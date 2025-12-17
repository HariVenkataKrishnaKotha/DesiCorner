using DesiCorner.Contracts.Reviews;
using DesiCorner.Services.ProductAPI.Data;
using DesiCorner.Services.ProductAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DesiCorner.Services.ProductAPI.Services;

public class ReviewService : IReviewService
{
    private readonly ProductDbContext _context;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(ProductDbContext context, ILogger<ReviewService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<ReviewDto>> GetProductReviewsAsync(
        Guid productId,
        int page = 1,
        int pageSize = 10,
        string sortBy = "newest",
        CancellationToken ct = default)
    {
        var query = _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .AsQueryable();

        // Apply sorting
        query = sortBy.ToLower() switch
        {
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "highest" => query.OrderByDescending(r => r.Rating),
            "lowest" => query.OrderBy(r => r.Rating),
            "helpful" => query.OrderByDescending(r => r.HelpfulCount),
            _ => query.OrderByDescending(r => r.CreatedAt) // newest (default)
        };

        var reviews = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => MapToDto(r))
            .ToListAsync(ct);

        return reviews;
    }

    public async Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId, CancellationToken ct = default)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        return review == null ? null : MapToDto(review);
    }

    public async Task<ReviewSummaryDto> GetReviewSummaryAsync(Guid productId, CancellationToken ct = default)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .ToListAsync(ct);

        var totalReviews = reviews.Count;
        var averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;

        return new ReviewSummaryDto
        {
            ProductId = productId,
            AverageRating = Math.Round(averageRating, 1),
            TotalReviews = totalReviews,
            FiveStarCount = reviews.Count(r => r.Rating == 5),
            FourStarCount = reviews.Count(r => r.Rating == 4),
            ThreeStarCount = reviews.Count(r => r.Rating == 3),
            TwoStarCount = reviews.Count(r => r.Rating == 2),
            OneStarCount = reviews.Count(r => r.Rating == 1)
        };
    }

    public async Task<ReviewDto?> GetUserReviewForProductAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, ct);

        return review == null ? null : MapToDto(review);
    }

    public async Task<ReviewDto> CreateReviewAsync(
        CreateReviewDto dto,
        Guid userId,
        string userName,
        string? userEmail,
        CancellationToken ct = default)
    {
        // Check if product exists
        var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId, ct);
        if (!productExists)
        {
            throw new ArgumentException("Product not found");
        }

        // Check if user already reviewed this product
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.UserId == userId, ct);

        if (existingReview != null)
        {
            throw new InvalidOperationException("You have already reviewed this product");
        }

        var review = new Review
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            UserId = userId,
            UserName = userName,
            UserEmail = userEmail,
            Rating = dto.Rating,
            Title = dto.Title,
            Comment = dto.Comment,
            IsVerifiedPurchase = false, // TODO: Check order history
            IsApproved = true, // Auto-approve for now
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        // Update product rating aggregation
        await UpdateProductRatingAsync(dto.ProductId, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Review created for product {ProductId} by user {UserId}", dto.ProductId, userId);

        return MapToDto(review);
    }

    public async Task<ReviewDto?> UpdateReviewAsync(UpdateReviewDto dto, Guid userId, CancellationToken ct = default)
    {
        var review = await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == dto.Id && r.UserId == userId, ct);

        if (review == null)
        {
            return null;
        }

        review.Rating = dto.Rating;
        review.Title = dto.Title;
        review.Comment = dto.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        // Update product rating aggregation
        await UpdateProductRatingAsync(review.ProductId, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Review {ReviewId} updated by user {UserId}", dto.Id, userId);

        return MapToDto(review);
    }

    public async Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin = false, CancellationToken ct = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review == null)
        {
            return false;
        }

        // Only allow deletion by owner or admin
        if (review.UserId != userId && !isAdmin)
        {
            return false;
        }

        var productId = review.ProductId;

        _context.Reviews.Remove(review);

        // Update product rating aggregation
        await UpdateProductRatingAsync(productId, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Review {ReviewId} deleted", reviewId);

        return true;
    }

    public async Task<bool> VoteReviewAsync(Guid reviewId, Guid userId, bool isHelpful, CancellationToken ct = default)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, ct);

        if (review == null)
        {
            return false;
        }

        // Check if user already voted on this review
        var existingVote = await _context.ReviewVotes
            .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId, ct);

        if (existingVote != null)
        {
            // User already voted - check if they're changing their vote
            if (existingVote.IsHelpful == isHelpful)
            {
                // Same vote - remove it (toggle off)
                _context.ReviewVotes.Remove(existingVote);

                if (isHelpful)
                    review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                else
                    review.NotHelpfulCount = Math.Max(0, review.NotHelpfulCount - 1);
            }
            else
            {
                // Different vote - switch the vote
                existingVote.IsHelpful = isHelpful;
                existingVote.UpdatedAt = DateTime.UtcNow;

                if (isHelpful)
                {
                    review.HelpfulCount++;
                    review.NotHelpfulCount = Math.Max(0, review.NotHelpfulCount - 1);
                }
                else
                {
                    review.NotHelpfulCount++;
                    review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                }
            }
        }
        else
        {
            // New vote
            var vote = new ReviewVote
            {
                Id = Guid.NewGuid(),
                ReviewId = reviewId,
                UserId = userId,
                IsHelpful = isHelpful,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReviewVotes.Add(vote);

            if (isHelpful)
                review.HelpfulCount++;
            else
                review.NotHelpfulCount++;
        }

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetReviewCountAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.Reviews
            .CountAsync(r => r.ProductId == productId && r.IsApproved, ct);
    }

    // Helper method to update product's average rating and review count
    private async Task UpdateProductRatingAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null) return;

        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId && r.IsApproved)
            .ToListAsync(ct);

        product.ReviewCount = reviews.Count;
        product.AverageRating = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;
    }

    // Helper method to map entity to DTO
    private static ReviewDto MapToDto(Review review)
    {
        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = review.UserName,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            HelpfulCount = review.HelpfulCount,
            NotHelpfulCount = review.NotHelpfulCount,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}