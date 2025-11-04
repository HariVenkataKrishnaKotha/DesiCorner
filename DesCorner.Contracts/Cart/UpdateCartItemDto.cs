using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DesiCorner.Contracts.Cart;

public class UpdateCartItemDto
{
    [Required]
    public Guid CartItemId { get; set; }

    [Required]
    [Range(1, 50)]
    public int Quantity { get; set; }
}
