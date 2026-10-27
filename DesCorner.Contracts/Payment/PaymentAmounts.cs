namespace DesiCorner.Contracts.Payment;

/// <summary>
/// Single definition of how an order total maps to a Stripe charge. PaymentAPI uses it to
/// create the PaymentIntent and OrderAPI uses it to check what was paid, so both agree.
/// </summary>
public static class PaymentAmounts
{
    /// <summary>The store's currency. Cart prices carry no currency of their own.</summary>
    public const string Currency = "usd";

    /// <summary>Converts a decimal amount to the smallest currency unit (cents).</summary>
    public static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
