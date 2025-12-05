using DesiCorner.Contracts.Payment;
using DesiCorner.Services.PaymentAPI.Data;
using DesiCorner.Services.PaymentAPI.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DesiCorner.Services.PaymentAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentService(
        PaymentDbContext context,
        ILogger<PaymentService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        PaymentIntentRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating payment intent for OrderId: {OrderId}, Amount: {Amount}",
                request.OrderId, request.Amount);

            // Convert amount to cents (Stripe requires smallest currency unit)
            var amountInCents = (long)(request.Amount * 100);

            // Create Stripe Payment Intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = request.Currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true, // Enables card, wallets, etc.
                },
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", request.OrderId.ToString() }
                }
            };

            // Add custom metadata if provided
            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    options.Metadata[kvp.Key] = kvp.Value;
                }
            }

            var service = new PaymentIntentService();
            var paymentIntent = await service.CreateAsync(options, cancellationToken: ct);

            _logger.LogInformation("Stripe Payment Intent created: {PaymentIntentId}, Status: {Status}",
                paymentIntent.Id, paymentIntent.Status);

            // Save payment record to database
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PaymentIntentId = paymentIntent.Id,
                OrderId = request.OrderId,
                AmountInCents = amountInCents,
                Amount = request.Amount,
                Currency = request.Currency.ToLower(),
                Status = paymentIntent.Status,
                ClientSecret = paymentIntent.ClientSecret,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Payment record saved to database: {PaymentId}", payment.Id);

            return new PaymentIntentResponseDto
            {
                PaymentIntentId = paymentIntent.Id,
                ClientSecret = paymentIntent.ClientSecret,
                Status = paymentIntent.Status,
                Amount = request.Amount
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating payment intent: {Error}", ex.Message);
            throw new InvalidOperationException($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            throw;
        }
    }

    public async Task<ConfirmPaymentDto> VerifyPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Verifying payment: {PaymentIntentId}", paymentIntentId);

            // Retrieve payment intent from Stripe
            var service = new PaymentIntentService();
            var paymentIntent = await service.GetAsync(paymentIntentId, cancellationToken: ct);

            _logger.LogInformation("Payment Intent Status: {Status}", paymentIntent.Status);

            // Update our database record
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);

            if (payment != null)
            {
                payment.Status = paymentIntent.Status;
                payment.PaymentMethodId = paymentIntent.PaymentMethodId;
                payment.UpdatedAt = DateTime.UtcNow;

                // If succeeded, save charge ID
                if (paymentIntent.Status == "succeeded" && paymentIntent.LatestChargeId != null)
                {
                    payment.ChargeId = paymentIntent.LatestChargeId;
                }

                // If failed, save error
                if (paymentIntent.LastPaymentError != null)
                {
                    payment.ErrorMessage = paymentIntent.LastPaymentError.Message;
                    payment.LastPaymentErrorCode = paymentIntent.LastPaymentError.Code;
                }

                await _context.SaveChangesAsync(ct);
            }

            // Extract OrderId from metadata
            Guid? orderId = null;
            if (paymentIntent.Metadata != null &&
                paymentIntent.Metadata.TryGetValue("order_id", out var orderIdStr))
            {
                Guid.TryParse(orderIdStr, out var parsedOrderId);
                orderId = parsedOrderId;
            }

            return new ConfirmPaymentDto
            {
                PaymentIntentId = paymentIntentId,
                OrderId = orderId ?? payment?.OrderId ?? Guid.Empty
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error verifying payment: {Error}", ex.Message);
            throw new InvalidOperationException($"Stripe error: {ex.StripeError?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            throw;
        }
    }

    public async Task HandleWebhookEventAsync(
        string paymentIntentId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Handling webhook for PaymentIntent: {PaymentIntentId}, Status: {Status}",
                paymentIntentId, status);

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for webhook: {PaymentIntentId}", paymentIntentId);
                return;
            }

            payment.Status = status;
            payment.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Payment status updated via webhook: {PaymentIntentId} -> {Status}",
                paymentIntentId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event");
            throw;
        }
    }

    public async Task<Payment?> GetPaymentByIntentIdAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);
    }
}