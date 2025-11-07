using Microsoft.AspNetCore.Identity;

namespace DesiCorner.AuthServer.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    // Profile Information
    public string? DietaryPreference { get; set; } // Veg, Non-Veg, Vegan
    public int RewardPoints { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // OTP/2FA
    public string? PendingOtp { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public int OtpAttempts { get; set; }

    // Navigation Properties
    public ICollection<DeliveryAddress> DeliveryAddresses { get; set; } = new List<DeliveryAddress>();
}