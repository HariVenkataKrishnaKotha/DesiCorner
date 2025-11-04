using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Messages;

public class PaymentFailedMessage : BaseMessage
{
    public PaymentFailedMessage()
    {
        MessageType = nameof(PaymentFailedMessage);
    }

    public Guid OrderId { get; set; }
    public string PaymentIntentId { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public Guid UserId { get; set; }
}
