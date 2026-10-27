namespace DesiCorner.Services.OrderAPI.Services;

/// <summary>
/// The payment cannot pay for this order: the amount or currency does not match the cart,
/// or the PaymentIntent is already attached to another order. Returned as 409 Conflict.
/// </summary>
public class PaymentRejectedException : InvalidOperationException
{
    public PaymentRejectedException(string message) : base(message)
    {
    }
}
