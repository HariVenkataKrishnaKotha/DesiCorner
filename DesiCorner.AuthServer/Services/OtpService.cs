using StackExchange.Redis;
using System.Security.Cryptography;

namespace DesiCorner.AuthServer.Services;

public class OtpService : IOtpService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly ILogger<OtpService> _logger;

    private const int OTP_LENGTH = 6;
    private const int OTP_EXPIRY_MINUTES = 10;
    private const int MAX_ATTEMPTS = 3;

    public OtpService(
        IConnectionMultiplexer redis,
        IEmailService emailService,
        IConfiguration config,
        ILogger<OtpService> logger)
    {
        _redis = redis;
        _emailService = emailService;
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendOtpAsync(string identifier, string purpose, string deliveryMethod = "Email", CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();

            // Generate 6-digit OTP
            var otp = GenerateOtp();

            // Store in Redis with expiry
            var key = RedisKeys.Otp(identifier);
            var otpData = $"{otp}:{purpose}";
            await db.StringSetAsync(key, otpData, TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES));

            // Reset attempts counter
            var attemptsKey = RedisKeys.OtpAttempts(identifier);
            await db.KeyDeleteAsync(attemptsKey);

            // Send via selected method
            if (deliveryMethod.Equals("Email", StringComparison.OrdinalIgnoreCase))
            {
                var sent = await _emailService.SendOtpEmailAsync(identifier, otp, purpose, ct);
                if (sent)
                {
                    _logger.LogInformation("OTP sent via email to {Email} (Purpose: {Purpose})", identifier, purpose);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to send OTP email to {Email}", identifier);
                    return false;
                }
            }
            else if (deliveryMethod.Equals("SMS", StringComparison.OrdinalIgnoreCase))
            {
                // SMS via Twilio (future implementation)
                _logger.LogWarning("SMS delivery not implemented. OTP for {Phone}: {Otp} (Purpose: {Purpose})",
                    identifier, otp, purpose);
                return true; // Return true for development
            }
            else
            {
                _logger.LogError("Unknown delivery method: {Method}", deliveryMethod);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP to {Identifier}", identifier);
            return false;
        }
    }

    public async Task<(bool isValid, string? error)> ValidateOtpAsync(
        string identifier,
        string otp,
        CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();

            // Check attempts
            var attemptsKey = RedisKeys.OtpAttempts(identifier);
            var attempts = (int)await db.StringIncrementAsync(attemptsKey);

            if (attempts == 1)
            {
                await db.KeyExpireAsync(attemptsKey, TimeSpan.FromMinutes(OTP_EXPIRY_MINUTES));
            }

            if (attempts > MAX_ATTEMPTS)
            {
                return (false, "Too many failed attempts. Please request a new OTP.");
            }

            // Get stored OTP
            var key = RedisKeys.Otp(identifier);
            var storedData = await db.StringGetAsync(key);

            if (storedData.IsNullOrEmpty)
            {
                return (false, "OTP expired or not found. Please request a new one.");
            }

            var parts = storedData.ToString().Split(':');
            var storedOtp = parts[0];

            if (storedOtp != otp)
            {
                var remaining = MAX_ATTEMPTS - attempts;
                return (false, $"Invalid OTP. {remaining} attempt(s) remaining.");
            }

            // Valid OTP - delete it (one-time use)
            await db.KeyDeleteAsync(key);
            await db.KeyDeleteAsync(attemptsKey);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate OTP for {Identifier}", identifier);
            return (false, "Validation error. Please try again.");
        }
    }

    public async Task<int> GetRemainingAttemptsAsync(string identifier, CancellationToken ct = default)
    {
        var db = _redis.GetDatabase();
        var attemptsKey = RedisKeys.OtpAttempts(identifier);
        var attempts = (int)await db.StringGetAsync(attemptsKey);
        return Math.Max(0, MAX_ATTEMPTS - attempts);
    }

    private static string GenerateOtp()
    {
        var number = RandomNumberGenerator.GetInt32(0, 1000000);
        return number.ToString("D6");
    }
}