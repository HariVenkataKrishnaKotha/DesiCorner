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
    private readonly PaymentIntentService _paymentIntentService;
    private readonly ICartClient _cartClient;

    public PaymentService(
        PaymentDbContext context,
        ILogger<PaymentService> logger,
        IConfiguration configuration,
        PaymentIntentService paymentIntentService,
        ICartClient cartClient)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _paymentIntentService = paymentIntentService;
        _cartClient = cartClient;
    }

    public async Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(
        PaymentIntentRequestDto request,
        PaymentOwner owner,
        CancellationToken ct = default)
    {
        try
        {
            // The amount charged is the caller's cart total as CartAPI computes it.
            // request.Amount and request.Currency are client-supplied and only logged.
            var cart = await _cartClient.GetCallerCartAsync(ct);
            if (cart == null || cart.Items.Count == 0 || cart.Total <= 0)
            {
                throw new InvalidOperationException("Cart is empty or unavailable. Add items before paying.");
            }

            var amount = cart.Total;
            var amountInCents = PaymentAmounts.ToMinorUnits(amount);
            var currency = PaymentAmounts.Currency;

            if (request.Amount != amount || !string.Equals(request.Currency, currency, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Client requested {RequestedAmount} {RequestedCurrency} but cart total is {CartTotal} {Currency}; charging the cart total",
                    request.Amount, request.Currency, amount, currency);
            }

            _logger.LogInformation("Creating payment intent for OrderId: {OrderId}, Amount: {Amount}",
                request.OrderId, amount);

            // Create Stripe Payment Intent
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountInCents,
                Currency = currency,
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

            var paymentIntent = await _paymentIntentService.CreateAsync(options, cancellationToken: ct);

            _logger.LogInformation("Stripe Payment Intent created: {PaymentIntentId}, Status: {Status}",
                paymentIntent.Id, paymentIntent.Status);

            // Save payment record to database
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                PaymentIntentId = paymentIntent.Id,
                OrderId = request.OrderId,
                UserId = owner.UserId,
                GuestSessionId = owner.GuestSessionId,
                AmountInCents = amountInCents,
                Amount = amount,
                Currency = currency,
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
                Amount = amount
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
            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId, cancellationToken: ct);

            _logger.LogInformation("Payment Intent Status: {Status}", paymentIntent.Status);

            // Update our database record
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);

            if (payment != null)
            {
                payment.Status = paymentIntent.Status;
                payment.PaymentMethodId = paymentIntent.PaymentMethodId;

                // Record what Stripe says the intent charges, not what was requested
                payment.AmountInCents = paymentIntent.Amount;
                payment.Amount = paymentIntent.Amount / 100m;
                payment.Currency = paymentIntent.Currency;
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

    public async Task<bool> HandleWebhookEventAsync(
        string eventId,
        string eventType,
        string paymentIntentId,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Handling webhook {EventId} for PaymentIntent: {PaymentIntentId}, Status: {Status}",
                eventId, paymentIntentId, status);

            // Stripe delivers events at least once; apply each event id only once
            if (await _context.ProcessedStripeEvents.AnyAsync(e => e.EventId == eventId, ct))
            {
                _logger.LogInformation("Webhook event {EventId} already processed, skipping", eventId);
                return false;
            }

            // Recorded in the same SaveChanges as the payment update, so both happen or neither does
            _context.ProcessedStripeEvents.Add(new ProcessedStripeEvent
            {
                EventId = eventId,
                EventType = eventType,
                ProcessedAt = DateTime.UtcNow
            });

            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for webhook: {PaymentIntentId}", paymentIntentId);
            }
            else
            {
                payment.Status = status;
                payment.UpdatedAt = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                if (!await IsEventRecordedAsync(eventId, ct))
                {
                    throw;
                }

                // A concurrent delivery of the same event won the insert
                _logger.LogInformation("Webhook event {EventId} processed concurrently, skipping", eventId);
                return false;
            }

            if (payment != null)
            {
                _logger.LogInformation("Payment status updated via webhook: {PaymentIntentId} -> {Status}",
                    paymentIntentId, status);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling webhook event");
            throw;
        }
    }

    private async Task<bool> IsEventRecordedAsync(string eventId, CancellationToken ct)
    {
        _context.ChangeTracker.Clear();
        return await _context.ProcessedStripeEvents.AnyAsync(e => e.EventId == eventId, ct);
    }

    public async Task<Payment?> GetPaymentByIntentIdAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId, ct);
    }
}