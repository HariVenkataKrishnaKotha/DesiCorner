using DesiCorner.Contracts.Reviews;

namespace DesiCorner.Services.ProductAPI.Tests;

/// <summary>
/// H1: review endpoints must act as the user in the validated token, not as whoever
/// a client-supplied X-Forwarded-* header names.
/// </summary>
public class ReviewIdentityTests
{
    private static readonly Guid Victim = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Dictionary<string, string> SpoofedHeaders = new()
    {
        ["X-Forwarded-UserId"] = Victim.ToString(),
        ["X-Forwarded-Email"] = "victim@example.test"
    };

    [Fact]
    public async Task Create_review_ignores_spoofed_identity_headers()
    {
        var service = new RecordingReviewService();
        var caller = Guid.NewGuid();
        var controller = ReviewsControllerHarness.Create(service, caller, SpoofedHeaders);

        await controller.CreateReview(new CreateReviewDto { ProductId = Guid.NewGuid() });

        var call = Assert.Single(service.Calls);
        Assert.Equal(caller, call.UserId);
        Assert.Equal("real.user@example.test", call.UserEmail);
    }

    [Fact]
    public async Task Delete_review_ignores_spoofed_user_header()
    {
        var service = new RecordingReviewService();
        var caller = Guid.NewGuid();
        var controller = ReviewsControllerHarness.Create(service, caller, SpoofedHeaders);

        await controller.DeleteReview(Guid.NewGuid());

        Assert.Equal(caller, Assert.Single(service.Calls).UserId);
    }
}
