using System.Reflection;
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
                var settings = EventStoreClientSettings
                    .Create(configuration.GetConnectionString("EventStore"));

                settings.ConnectionName = "kuna.eventstore.seeder-" + Guid.NewGuid();

                return new EventStoreClient(settings);
            });


        return services;
    }
}
