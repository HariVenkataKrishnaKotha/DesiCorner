using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesiCorner.MessageBus.Messages;

namespace DesiCorner.MessageBus.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(T message, string queueOrTopicName, CancellationToken cancellationToken = default) where T : BaseMessage;
}
