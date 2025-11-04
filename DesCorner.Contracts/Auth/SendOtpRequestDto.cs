using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Auth;

public class SendOtpRequestDto
{
    public string? PhoneNumber { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string Purpose { get; set; } = "Verification"; // Verification, PasswordReset, Login

    [Required]
    public string DeliveryMethod { get; set; } = "Email"; // Email or SMS
}