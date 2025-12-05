namespace DesiCorner.Contracts.Payment;

public class VerifyPaymentResponse
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ErrorMessage { get; set; }
}