using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Cart;

public class AddToCartDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    [Range(1, 50)]
    public int Quantity { get; set; } = 1;

    public Guid? UserId { get; set; }

    public string? SessionId { get; set; } // For guest checkout
}
