using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Messages;

public class OrderConfirmedMessage : BaseMessage
{
    public OrderConfirmedMessage()
    {
        MessageType = nameof(OrderConfirmedMessage);
    }

    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public DateTime EstimatedDeliveryTime { get; set; }
}
