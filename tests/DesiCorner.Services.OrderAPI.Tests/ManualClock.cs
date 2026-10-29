namespace DesiCorner.Services.OrderAPI.Tests;

/// <summary>A TimeProvider whose clock moves only when a test advances it.</summary>
public sealed class ManualClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}
