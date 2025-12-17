namespace DesiCorner.Contracts.Reviews;

public class ReviewVoteDto
{
    public Guid ReviewId { get; set; }
    public bool IsHelpful { get; set; }
}