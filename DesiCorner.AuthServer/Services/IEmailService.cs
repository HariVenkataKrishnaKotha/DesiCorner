namespace DesiCorner.AuthServer.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
    Task<bool> SendOtpEmailAsync(string to, string otp, string purpose, CancellationToken ct = default);
}