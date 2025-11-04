using DesiCorner.MessageBus.Messages;
using DesiCorner.MessageBus.Redis;
using DesiCorner.MessageBus.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesiCorner.MessageBus.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Service Bus publisher and consumer services
    /// </summary>
    public static IServiceCollection AddDesiCornerServiceBus(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();
        services.AddSingleton<IServiceBusConsumer, ServiceBusConsumer>();
        return services;
    }

    /// <summary>
    /// Adds Redis cache service
    /// </summary>
    public static IServiceCollection AddDesiCornerRedis(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Redis connection
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]!));

        // Add cache service
        services.AddSingleton<ICacheService, CacheService>();

        return services;
    }

    /// <summary>
    /// Adds both Service Bus and Redis
    /// </summary>
    public static IServiceCollection AddDesiCornerMessageBus(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDesiCornerServiceBus();
        services.AddDesiCornerRedis(configuration);
        return services;
    }
}
