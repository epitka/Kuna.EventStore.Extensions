using System.Collections.Concurrent;
using System.Reflection;

namespace Kuna.EventStore.Seeder.Services;

public interface IEventTypeMapper
{
    Type MapFrom(string name);
}

//TODO: implement as singleton using Lazy

public class EventTypeMapper : IEventTypeMapper
{
    private static readonly ConcurrentDictionary<string, Type> TypeMap = new();

    /// <summary>
    /// Collects all events from assemblies provided and builds map that allows
    /// mapping of the event name to event type.
    /// </summary>
    /// <param name="eventDiscoveryFunc">Provides a way to discover events in assemblies provided.
    /// This can be based for example or marker interface (IAggregateEvent), folder name (Events) or event name convention (...Event), or some other way </param>
    public  EventTypeMapper(
        Assembly[] assembliesToScan,
        Func<Assembly[], Type[]> eventDiscoveryFunc)
    {

        if (assembliesToScan == null
            || !assembliesToScan.Any())
        {
            throw new ArgumentNullException(
                nameof(assembliesToScan),
                "No assemblies to scan for IAggregateEvent implmentations found.");
        }

        var eventTypes = eventDiscoveryFunc.Invoke(assembliesToScan);

        foreach (var eventType in eventTypes)
        {
            TypeMap.TryAdd(eventType.Name, eventType);
        }
    }

    public Type MapFrom(string name)
    {
        if (TypeMap.TryGetValue(name, out var eventType))
        {
            return eventType;
        }

        throw new InvalidOperationException($"Missing event type definition for event type name {name}");
    }
}
