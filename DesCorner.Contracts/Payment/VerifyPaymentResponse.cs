namespace DesiCorner.Contracts.Payment;

public class VerifyPaymentResponse
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    /// <summary>Amount Stripe charged, in the smallest currency unit. Null if unknown.</summary>
    public long? AmountInCents { get; set; }

    /// <summary>Currency Stripe charged in (lowercase ISO code). Null if unknown.</summary>
    public string? Currency { get; set; }
    public string? ErrorMessage { get; set; }
}