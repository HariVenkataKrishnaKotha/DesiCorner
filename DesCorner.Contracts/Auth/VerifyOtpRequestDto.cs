using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Auth;

public class VerifyOtpRequestDto
{
    [Required]
    public string Identifier { get; set; } = string.Empty; // Can be email or phone

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}
