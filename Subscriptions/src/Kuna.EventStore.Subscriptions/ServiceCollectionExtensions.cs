using Kuna.EventStore.Subscriptions.Catchup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;


namespace Kuna.EventStore.Subscriptions;

public static class ServiceCollectionExtensions
{
    private const string EventStoreConnectionStringName = "EventStore";

    public static IServiceCollection AddResolvedEventsSource(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        EventStoreSubscriptionSettings? esSettings = null;

       return services
           // don't want to take depdency on IOptions anywhere else hence doing it manually like this
           .AddSingleton(resolver =>
           {
                esSettings = configuration.GetRequiredSection("EventStore").Get<EventStoreSubscriptionSettings>();

                if (esSettings == null)
                {
                    throw new InvalidOperationException("EventStore settings not found.");
                }

                // Manually validate settings
                var validationResults = new List<ValidationResult>();
                Validator.ValidateObject(esSettings, new ValidationContext(esSettings));

                // TODO: clarify MaxSearchWindow vs ReadBatchSize
                if (esSettings.MaxSearchWindow > esSettings.ReadBatchSize)
                {
                    esSettings.MaxSearchWindow = esSettings.ReadBatchSize;
                }

                return esSettings;
           })
           .AddSingleton(
                provider =>
                {
                    var connectionString = configuration.GetConnectionString(EventStoreConnectionStringName)
                                           ?? throw new InvalidOperationException($"No connection string named '{EventStoreConnectionStringName}' found.");

                    var settings = EventStoreClientSettings.Create(connectionString);

                    settings.ConnectionName = esSettings!.SubscriptionName;

                    return new EventStoreClient(settings);
                })
           .AddSingleton<IResolvedEventSource, ResolvedEventSource>();
    }
}
