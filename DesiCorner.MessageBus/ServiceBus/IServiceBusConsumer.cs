using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.ServiceBus;

public interface IServiceBusConsumer
{
    Task StartAsync(string queueOrTopicName, string? subscriptionName, Func<string, Task> messageHandler, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
