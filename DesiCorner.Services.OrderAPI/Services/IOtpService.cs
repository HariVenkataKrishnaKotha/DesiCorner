namespace DesiCorner.Services.OrderAPI.Services;

public interface IOtpService
{
    Task<bool> SendOtpAsync(string email, string purpose, CancellationToken ct = default);
    Task<bool> VerifyOtpAsync(string email, string otpCode, CancellationToken ct = default);
}