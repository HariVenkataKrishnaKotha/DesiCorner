using DesiCorner.Contracts.Reviews;

namespace DesiCorner.Services.ProductAPI.Services;

public interface IReviewService
{
    // Get reviews for a product (with pagination)
    Task<List<ReviewDto>> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10, string sortBy = "newest", CancellationToken ct = default);

    // Get a single review by ID
    Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId, CancellationToken ct = default);

    // Get review summary/statistics for a product
    Task<ReviewSummaryDto> GetReviewSummaryAsync(Guid productId, CancellationToken ct = default);

    // Check if user has already reviewed a product
    Task<ReviewDto?> GetUserReviewForProductAsync(Guid productId, Guid userId, CancellationToken ct = default);

    // Create a new review
    Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, Guid userId, string userName, string? userEmail, CancellationToken ct = default);

    // Update an existing review (only by the author)
    Task<ReviewDto?> UpdateReviewAsync(UpdateReviewDto dto, Guid userId, CancellationToken ct = default);

    // Delete a review (by author or admin)
    Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin = false, CancellationToken ct = default);

    // Vote on a review (helpful/not helpful)
    Task<bool> VoteReviewAsync(Guid reviewId, Guid userId, bool isHelpful, CancellationToken ct = default);

    // Get total review count for a product
    Task<int> GetReviewCountAsync(Guid productId, CancellationToken ct = default);
}