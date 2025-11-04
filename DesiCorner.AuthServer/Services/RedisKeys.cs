namespace DesiCorner.AuthServer.Services;

public static class RedisKeys
{
    // Rate Limiting
    public static string LoginRateLimit(string ip) => $"rl:login:{ip}";
    public static string RegisterRateLimit(string ip) => $"rl:register:{ip}";

    // OTP - Works for both email and phone
    public static string Otp(string identifier) => $"otp:{identifier}";
    public static string OtpAttempts(string identifier) => $"otp:attempts:{identifier}";

    // Password Reset
    public static string PasswordResetToken(string token) => $"pwd:reset:{token}";

    // User Sessions
    public static string UserSession(Guid userId) => $"session:{userId}";
}