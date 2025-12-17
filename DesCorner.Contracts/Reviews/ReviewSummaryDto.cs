namespace DesiCorner.Contracts.Reviews;

public class ReviewSummaryDto
{
    public Guid ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }

    // Rating distribution (count of each star rating)
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }

    // Percentages for progress bars
    public double FiveStarPercent => TotalReviews > 0 ? (double)FiveStarCount / TotalReviews * 100 : 0;
    public double FourStarPercent => TotalReviews > 0 ? (double)FourStarCount / TotalReviews * 100 : 0;
    public double ThreeStarPercent => TotalReviews > 0 ? (double)ThreeStarCount / TotalReviews * 100 : 0;
    public double TwoStarPercent => TotalReviews > 0 ? (double)TwoStarCount / TotalReviews * 100 : 0;
    public double OneStarPercent => TotalReviews > 0 ? (double)OneStarCount / TotalReviews * 100 : 0;
}
