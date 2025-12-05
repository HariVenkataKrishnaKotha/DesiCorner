using DesiCorner.Contracts.Payment;

namespace DesiCorner.Services.PaymentAPI.Services;

public interface IPaymentService
{
    /// <summary>
    /// Create a new Stripe Payment Intent
    /// </summary>
    Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        PaymentIntentRequestDto request,
        CancellationToken ct = default);

    /// <summary>
    /// Verify payment status by Payment Intent ID
    /// </summary>
    Task<ConfirmPaymentDto> VerifyPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default);

    /// <summary>
    /// Update payment record from Stripe webhook
    /// </summary>
    Task HandleWebhookEventAsync(
        string paymentIntentId,
        string status,
        CancellationToken ct = default);

    /// <summary>
    /// Get payment record by Payment Intent ID
    /// </summary>
    Task<Models.Payment?> GetPaymentByIntentIdAsync(
        string paymentIntentId,
        CancellationToken ct = default);
}