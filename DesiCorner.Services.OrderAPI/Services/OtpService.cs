using DesiCorner.Contracts.Auth;
using DesiCorner.Contracts.Common;

namespace DesiCorner.Services.OrderAPI.Services;

public class OtpService : IOtpService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OtpService> _logger;

    public OtpService(IHttpClientFactory httpClientFactory, ILogger<OtpService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> SendOtpAsync(string email, string purpose, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthAPI");

            var request = new SendOtpRequestDto
            {
                Email = email,
                Purpose = purpose,
                DeliveryMethod = "Email"
            };

            var response = await client.PostAsJsonAsync("/api/account/send-otp", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send OTP. Status: {Status}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ResponseDto>(ct);
            return result?.IsSuccess == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending OTP to {Email}", email);
            return false;
        }
    }

    public async Task<bool> VerifyOtpAsync(string email, string otpCode, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("AuthAPI");

            var request = new VerifyOtpRequestDto
            {
                Identifier = email,
                Otp = otpCode
            };

            var response = await client.PostAsJsonAsync("/api/account/verify-otp", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to verify OTP. Status: {Status}", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<ResponseDto>(ct);
            return result?.IsSuccess == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for {Email}", email);
            return false;
        }
    }
}