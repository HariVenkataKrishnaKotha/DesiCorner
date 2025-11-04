namespace DesiCorner.AuthServer.Services;

public interface IOtpService
{
    Task<bool> SendOtpAsync(string identifier, string purpose, string deliveryMethod = "Email", CancellationToken ct = default);
    Task<(bool isValid, string? error)> ValidateOtpAsync(string identifier, string otp, CancellationToken ct = default);
    Task<int> GetRemainingAttemptsAsync(string identifier, CancellationToken ct = default);
}