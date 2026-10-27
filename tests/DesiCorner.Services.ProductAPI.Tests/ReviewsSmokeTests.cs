using DesiCorner.Contracts.Reviews;

namespace DesiCorner.Services.ProductAPI.Tests;

public class ReviewsSmokeTests
{
    [Fact]
    public async Task Review_is_created_for_the_authenticated_user()
    {
        var service = new RecordingReviewService();
        var userId = Guid.NewGuid();
        var controller = ReviewsControllerHarness.Create(service, userId);

        await controller.CreateReview(new CreateReviewDto { ProductId = Guid.NewGuid() });

        var call = Assert.Single(service.Calls);
        Assert.Equal(userId, call.UserId);
        Assert.Equal("real.user@example.test", call.UserEmail);
    }
}
