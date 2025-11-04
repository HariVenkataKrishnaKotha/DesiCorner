using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Messages;

public class OrderCreatedMessage : BaseMessage
{
    public OrderCreatedMessage()
    {
        MessageType = nameof(OrderCreatedMessage);
    }

    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<OrderItemMessage> Items { get; set; } = new();
    public string DeliveryAddress { get; set; } = string.Empty;
}

public class OrderItemMessage
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
