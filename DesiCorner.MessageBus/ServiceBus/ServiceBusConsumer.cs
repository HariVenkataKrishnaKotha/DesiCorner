using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DesiCorner.MessageBus.ServiceBus;

public class ServiceBusConsumer : IServiceBusConsumer, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusConsumer> _logger;
    private ServiceBusProcessor? _processor;

    public ServiceBusConsumer(IConfiguration configuration, ILogger<ServiceBusConsumer> logger)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is not configured");

        _client = new ServiceBusClient(connectionString);
        _logger = logger;
    }

    public async Task StartAsync(
        string queueOrTopicName,
        string? subscriptionName,
        Func<string, Task> messageHandler,
        CancellationToken cancellationToken = default)
    {
        var options = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        };

        _processor = string.IsNullOrEmpty(subscriptionName)
            ? _client.CreateProcessor(queueOrTopicName, options)
            : _client.CreateProcessor(queueOrTopicName, subscriptionName, options);

        _processor.ProcessMessageAsync += async args =>
        {
            try
            {
                var messageBody = args.Message.Body.ToString();
                _logger.LogInformation(
                    "Processing message {MessageId} from {Source}",
                    args.Message.MessageId,
                    queueOrTopicName);

                await messageHandler(messageBody);

                await args.CompleteMessageAsync(args.Message, cancellationToken);

                _logger.LogInformation(
                    "Completed processing message {MessageId}",
                    args.Message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message {MessageId}",
                    args.Message.MessageId);

                // Move to dead-letter queue
                await args.DeadLetterMessageAsync(args.Message, cancellationToken: cancellationToken);
            }
        };

        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception,
                "Error in Service Bus processor for {Source}",
                queueOrTopicName);
            return Task.CompletedTask;
        };

        await _processor.StartProcessingAsync(cancellationToken);
        _logger.LogInformation("Started Service Bus consumer for {Source}", queueOrTopicName);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_processor != null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            _logger.LogInformation("Stopped Service Bus consumer");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_processor != null)
        {
            await _processor.DisposeAsync();
        }
        await _client.DisposeAsync();
    }
}
