using DesiCorner.Contracts.Common;
using DesiCorner.Contracts.Payment;
using DesiCorner.Services.PaymentAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace DesiCorner.Services.PaymentAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;
    private readonly IConfiguration _configuration;

    public PaymentController(
        IPaymentService paymentService,
        ILogger<PaymentController> logger,
        IConfiguration configuration)
    {
        _paymentService = paymentService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Create a payment intent for an order
    /// </summary>
    /// <remarks>
    /// This endpoint creates a Stripe Payment Intent and returns a client secret.
    /// The client secret is used by the frontend to complete payment.
    /// 
    /// **Flow:**
    /// 1. Frontend calls this endpoint with order amount
    /// 2. PaymentAPI creates Stripe Payment Intent
    /// 3. Returns client_secret to frontend
    /// 4. Frontend uses client_secret with Stripe.js to collect payment
    /// </remarks>
    [HttpPost("create-intent")]
    [AllowAnonymous] // Allow both authenticated and guest users
    public async Task<IActionResult> CreatePaymentIntent(
        [FromBody] PaymentIntentRequestDto request,
        CancellationToken ct)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Invalid request data"
                });
            }

            var result = await _paymentService.CreatePaymentIntentAsync(request, ct);

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Message = "Payment intent created successfully",
                Result = result
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create payment intent");
            return BadRequest(new ResponseDto
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment intent");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while creating payment intent"
            });
        }
    }

    /// <summary>
    /// Verify payment status
    /// </summary>
    /// <remarks>
    /// This endpoint verifies the payment status with Stripe.
    /// Called by OrderAPI before finalizing an order.
    /// 
    /// **Flow:**
    /// 1. Frontend completes payment with Stripe
    /// 2. Frontend calls OrderAPI to create order
    /// 3. OrderAPI calls this endpoint to verify payment succeeded
    /// 4. If verified, OrderAPI creates the order
    /// </remarks>
    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyPayment(
        [FromBody] VerifyPaymentRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PaymentIntentId))
            {
                return BadRequest(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "PaymentIntentId is required"
                });
            }

            var confirmation = await _paymentService.VerifyPaymentAsync(
                request.PaymentIntentId,
                ct);

            // Get payment record to check status
            var payment = await _paymentService.GetPaymentByIntentIdAsync(
                request.PaymentIntentId,
                ct);

            if (payment == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Payment not found"
                });
            }

            // Check if payment succeeded
            var isSuccess = payment.Status == "succeeded";

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new VerifyPaymentResponse
                {
                    IsSuccess = isSuccess,
                    Status = payment.Status,
                    Amount = payment.Amount,
                    ErrorMessage = payment.ErrorMessage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while verifying payment"
            });
        }
    }

    /// <summary>
    /// Get payment details by Payment Intent ID
    /// </summary>
    [HttpGet("{paymentIntentId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPayment(
        string paymentIntentId,
        CancellationToken ct)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByIntentIdAsync(
                paymentIntentId,
                ct);

            if (payment == null)
            {
                return NotFound(new ResponseDto
                {
                    IsSuccess = false,
                    Message = "Payment not found"
                });
            }

            return Ok(new ResponseDto
            {
                IsSuccess = true,
                Result = new
                {
                    payment.PaymentIntentId,
                    payment.Status,
                    payment.Amount,
                    payment.Currency,
                    payment.OrderId,
                    payment.CreatedAt,
                    payment.ErrorMessage
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving payment"
            });
        }
    }

    /// <summary>
    /// Stripe webhook endpoint
    /// </summary>
    /// <remarks>
    /// This endpoint receives events from Stripe when payment status changes.
    /// Stripe will call this webhook with events like:
    /// - payment_intent.succeeded
    /// - payment_intent.payment_failed
    /// - payment_intent.canceled
    /// 
    /// **Setup:**
    /// 1. Go to Stripe Dashboard → Developers → Webhooks
    /// 2. Add endpoint: https://yourdomain.com/api/payment/webhook
    /// 3. Select events: payment_intent.*
    /// 4. Copy webhook signing secret to appsettings.json
    /// </remarks>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct)
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync(ct);

        try
        {
            var stripeEvent = EventUtility.ParseEvent(json);
            var signatureHeader = Request.Headers["Stripe-Signature"];

            // Verify webhook signature (if webhook secret is configured)
            var webhookSecret = _configuration["Stripe:WebhookSecret"];
            if (!string.IsNullOrEmpty(webhookSecret))
            {
                try
                {
                    stripeEvent = EventUtility.ConstructEvent(
                        json,
                        signatureHeader,
                        webhookSecret
                    );
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Webhook signature verification failed");
                    return BadRequest();
                }
            }

            _logger.LogInformation("Webhook received: {EventType}", stripeEvent.Type);

            // Handle payment_intent events
            if (stripeEvent.Type.StartsWith("payment_intent."))
            {
                var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                if (paymentIntent != null)
                {
                    await _paymentService.HandleWebhookEventAsync(
                        paymentIntent.Id,
                        paymentIntent.Status,
                        ct
                    );

                    _logger.LogInformation(
                        "Processed webhook for PaymentIntent: {PaymentIntentId}, Status: {Status}",
                        paymentIntent.Id,
                        paymentIntent.Status
                    );
                }
            }

            return Ok();
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return BadRequest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Get Stripe publishable key (for frontend)
    /// </summary>
    [HttpGet("config")]
    [AllowAnonymous]
    public IActionResult GetStripeConfig()
    {
        var publishableKey = _configuration["Stripe:PublishableKey"];

        if (string.IsNullOrEmpty(publishableKey))
        {
            return StatusCode(500, new ResponseDto
            {
                IsSuccess = false,
                Message = "Stripe publishable key not configured"
            });
        }

        return Ok(new ResponseDto
        {
            IsSuccess = true,
            Result = new
            {
                PublishableKey = publishableKey
            }
        });
    }
}