using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.Contracts.Auth;

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneNumberConfirmed { get; set; }
    public string? DietaryPreference { get; set; }
    public int RewardPoints { get; set; }
    public List<DeliveryAddressDto> Addresses { get; set; } = new();
    public List<string> Roles { get; set; } = new();
}

public class DeliveryAddressDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty; // Home, Work, etc.
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
