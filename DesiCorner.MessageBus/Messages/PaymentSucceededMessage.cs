using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Messages;

public class PaymentSucceededMessage : BaseMessage
{
    public PaymentSucceededMessage()
    {
        MessageType = nameof(PaymentSucceededMessage);
    }

    public Guid OrderId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public Guid UserId { get; set; }
}
