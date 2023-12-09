using System.Reflection;
using System.Resources;
using EventStore.Client;
using Kuna.EventStore.Seeder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kuna.EventStore.Seeder;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddEventStoreSeeder(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly[] assembliesWithAggregateEvents,
        Func<Assembly[], Type[]> aggregateEventsDiscoverFunc)
    {
        services.AddSingleton<IEventTypeMapper>(sp => new EventTypeMapper(assembliesWithAggregateEvents, aggregateEventsDiscoverFunc))
                .AddSingleton<IEventStoreSerializer, JsonEventStoreSerializer>()
                .AddSingleton<IEventMetadataFactory, EventMetadataFactory>()
                .AddSingleton<IEventDataFactory, EventDataFactory>()
                .AddSingleton<IWorkerFactory, WorkerFactory>()
                .AddSingleton<Runner>();

        services.AddSingleton<EventStoreClient>(
            sp =>
            {
                var connectionString = configuration.GetConnectionString("EventStore");

                if (connectionString is null)
                {
                    throw new InvalidOperationException("EventStore connection string is not found.");
                }

                var settings = EventStoreClientSettings
                    .Create(connectionString);

                settings.ConnectionName = "kuna.eventstore.seeder-" + Guid.NewGuid();

                return new EventStoreClient(settings);
            });

        return services;
    }
}
