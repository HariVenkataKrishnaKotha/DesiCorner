using System.Security.Claims;
using DesiCorner.Contracts.Reviews;
using DesiCorner.Services.ProductAPI.Controllers;
using DesiCorner.Services.ProductAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace DesiCorner.Services.ProductAPI.Tests;

/// <summary>
/// Builds a ReviewsController whose request carries the given principal and headers,
/// exactly as JwtBearer (with MapInboundClaims = false) would populate it.
/// </summary>
public static class ReviewsControllerHarness
{
    public static ReviewsController Create(
        RecordingReviewService service,
        Guid? authenticatedUserId,
        IDictionary<string, string>? headers = null)
    {
        var identity = authenticatedUserId is { } id
            ? new ClaimsIdentity(new[]
            {
                new Claim("sub", id.ToString()),
                new Claim("name", "Real User"),
                new Claim("email", "real.user@example.test")
            }, authenticationType: "Bearer", nameType: "name", roleType: "role")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        foreach (var (name, value) in headers ?? new Dictionary<string, string>())
        {
            httpContext.Request.Headers[name] = value;
        }

        return new ReviewsController(service, NullLogger<ReviewsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}

public sealed record ReviewCall(string Operation, Guid UserId, string? UserEmail = null);

public sealed class RecordingReviewService : IReviewService
{
    public List<ReviewCall> Calls { get; } = new();

    public Task<ReviewDto> CreateReviewAsync(CreateReviewDto dto, Guid userId, string userName, string? userEmail, CancellationToken ct = default)
    {
        Calls.Add(new ReviewCall("create", userId, userEmail));
        return Task.FromResult(new ReviewDto { Id = Guid.NewGuid(), ProductId = dto.ProductId, UserId = userId });
    }

    public Task<bool> DeleteReviewAsync(Guid reviewId, Guid userId, bool isAdmin = false, CancellationToken ct = default)
    {
        Calls.Add(new ReviewCall("delete", userId));
        return Task.FromResult(true);
    }

    public Task<ReviewDto?> GetUserReviewForProductAsync(Guid productId, Guid userId, CancellationToken ct = default)
    {
        Calls.Add(new ReviewCall("my-review", userId));
        return Task.FromResult<ReviewDto?>(null);
    }

    public Task<List<ReviewDto>> GetProductReviewsAsync(Guid productId, int page = 1, int pageSize = 10, string sortBy = "newest", CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ReviewDto?> GetReviewByIdAsync(Guid reviewId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ReviewSummaryDto> GetReviewSummaryAsync(Guid productId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ReviewDto?> UpdateReviewAsync(UpdateReviewDto dto, Guid userId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> VoteReviewAsync(Guid reviewId, Guid userId, bool isHelpful, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<int> GetReviewCountAsync(Guid productId, CancellationToken ct = default) =>
        throw new NotSupportedException();
}
