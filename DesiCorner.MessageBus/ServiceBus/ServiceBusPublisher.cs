using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using DesiCorner.MessageBus.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DesiCorner.MessageBus.ServiceBus;

public class ServiceBusPublisher : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ServiceBusPublisher(IConfiguration configuration, ILogger<ServiceBusPublisher> logger)
    {
        var connectionString = configuration["ServiceBus:ConnectionString"]
            ?? throw new InvalidOperationException("ServiceBus:ConnectionString is not configured");

        _client = new ServiceBusClient(connectionString);
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<T>(T message, string queueOrTopicName, CancellationToken cancellationToken = default) where T : BaseMessage
    {
        try
        {
            var sender = _client.CreateSender(queueOrTopicName);

            var messageBody = JsonSerializer.Serialize(message, _jsonOptions);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                MessageId = message.MessageId.ToString(),
                ContentType = "application/json",
                Subject = message.MessageType
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);

            _logger.LogInformation(
                "Published message {MessageType} with ID {MessageId} to {Destination}",
                message.MessageType,
                message.MessageId,
                queueOrTopicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish message {MessageType} with ID {MessageId} to {Destination}",
                message.MessageType,
                message.MessageId,
                queueOrTopicName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
