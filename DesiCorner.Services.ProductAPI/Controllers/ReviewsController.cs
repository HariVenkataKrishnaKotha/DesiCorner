using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Reviews;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DesiCorner.Services.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    /// <summary>
    /// Get reviews for a product with pagination and sorting
    /// </summary>
    [HttpGet("product/{productId}")]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "newest",
        CancellationToken ct = default)
    {
        try
        {
            var reviews = await _reviewService.GetProductReviewsAsync(productId, page, pageSize, sortBy, ct);
            var totalCount = await _reviewService.GetReviewCountAsync(productId, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new
                {
                    Reviews = reviews,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting reviews for product {ProductId}", productId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving reviews"
            });
        }
    }

    /// <summary>
    /// Get review summary/statistics for a product
    /// </summary>
    [HttpGet("product/{productId}/summary")]
    public async Task<IActionResult> GetReviewSummary(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var summary = await _reviewService.GetReviewSummaryAsync(productId, ct);
            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review summary for product {ProductId}", productId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving review summary"
            });
        }
    }

    /// <summary>
    /// Get a single review by ID
    /// </summary>
    [HttpGet("{reviewId}")]
    public async Task<IActionResult> GetReviewById(Guid reviewId, CancellationToken ct = default)
    {
        try
        {
            var review = await _reviewService.GetReviewByIdAsync(reviewId, ct);
            if (review == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Review not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = review
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting review {ReviewId}", reviewId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving review"
            });
        }
    }

    /// <summary>
    /// Get current user's review for a product (to check if already reviewed)
    /// </summary>
    [Authorize]
    [HttpGet("product/{productId}/my-review")]
    public async Task<IActionResult> GetMyReviewForProduct(Guid productId, CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var review = await _reviewService.GetUserReviewForProductAsync(productId, userId.Value, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = review // Can be null if user hasn't reviewed
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user review for product {ProductId}", productId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error retrieving review"
            });
        }
    }

    /// <summary>
    /// Create a new review (authenticated users only)
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto dto, CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            var userName = GetUserName();
            var userEmail = GetUserEmail();

            if (userId == null)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var review = await _reviewService.CreateReviewAsync(dto, userId.Value, userName, userEmail, ct);

            return CreatedAtAction(nameof(GetReviewById), new { reviewId = review.Id }, new ResponseDto
            {
                IsSuccess = true,
                Message = "Review submitted successfully",
                Result = review
            });
        }
        catch (InvalidOperationException ex)
        {
            // User already reviewed this product
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review for product {ProductId}", dto.ProductId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error submitting review"
            });
        }
    }

    /// <summary>
    /// Update an existing review (only by the author)
    /// </summary>
    [Authorize]
    [HttpPut("{reviewId}")]
    public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] UpdateReviewDto dto, CancellationToken ct = default)
    {
        if (reviewId != dto.Id)
        {
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = "ID mismatch"
            });
        }

        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var review = await _reviewService.UpdateReviewAsync(dto, userId.Value, ct);
            if (review == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Review not found or you don't have permission to edit it"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Review updated successfully",
                Result = review
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating review {ReviewId}", reviewId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error updating review"
            });
        }
    }

    /// <summary>
    /// Delete a review (by author or admin)
    /// </summary>
    [Authorize]
    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId, CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var isAdmin = User.IsInRole("Admin");
            var result = await _reviewService.DeleteReviewAsync(reviewId, userId.Value, isAdmin, ct);

            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Review not found or you don't have permission to delete it"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Review deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting review {ReviewId}", reviewId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error deleting review"
            });
        }
    }

    /// <summary>
    /// Vote on a review (helpful/not helpful)
    /// </summary>
    [Authorize]
    [HttpPost("{reviewId}/vote")]
    public async Task<IActionResult> VoteReview(Guid reviewId, [FromBody] ReviewVoteDto dto, CancellationToken ct = default)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "User not authenticated"
                });
            }

            var result = await _reviewService.VoteReviewAsync(reviewId, userId.Value, dto.IsHelpful, ct);
            if (!result)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Review not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Vote recorded"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error voting on review {ReviewId}", reviewId);
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Error recording vote"
            });
        }
    }

    // Helper methods to get user info from claims
    private Guid? GetUserId()
    {
        // Try forwarded header first (from Gateway)
        var forwardedUserId = Request.Headers["X-Forwarded-UserId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedUserId) && Guid.TryParse(forwardedUserId, out var guidFromHeader))
        {
            return guidFromHeader;
        }

        // Fall back to claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdClaim, out var guid))
        {
            return guid;
        }

        return null;
    }

    private string GetUserName()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value
            ?? User.FindFirst("name")?.Value
            ?? "Anonymous";
    }

    private string? GetUserEmail()
    {
        // Try forwarded header first (from Gateway)
        var forwardedEmail = Request.Headers["X-Forwarded-Email"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedEmail))
        {
            return forwardedEmail;
        }

        return User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("email")?.Value;
    }
}