using System.Text;
using EventStore.Client;
using Newtonsoft.Json;

namespace Kuna.EventStore.Seeder.Services;

public interface IEventStoreSerializer
{
    object? DeserializeData(ResolvedEvent resolvedEvent);

    IDictionary<string, string> DeserializeMetaData(ResolvedEvent resolvedEvent);

    (object? Event, Type EventType) Deserialize(ResolvedEvent resolvedEvent);

    byte[] Serialize(object obj);
}

public class JsonEventStoreSerializer : IEventStoreSerializer
{
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        TypeNameHandling = TypeNameHandling.None,
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Include,
    };

    private readonly IEventTypeMapper eventTypeMapper;

    public JsonEventStoreSerializer(IEventTypeMapper eventTypeMapper)
    {
        this.eventTypeMapper = eventTypeMapper;
    }

    public object? DeserializeData(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event == null)
        {
            return null;
        }

        var eventType = this.eventTypeMapper.MapFrom(resolvedEvent.Event.EventType ?? string.Empty);

        return DeserializeEvent(resolvedEvent, eventType);
    }

    public IDictionary<string, string> DeserializeMetaData(ResolvedEvent resolvedEvent)
    {
        return JsonConvert.DeserializeObject<Dictionary<string, string>>(
                   Encoding.UTF8.GetString(resolvedEvent.Event.Metadata.Span),
                   SerializerSettings)
               ?? new Dictionary<string, string>();
    }

    /// <summary>
    ///     This method is used by persistent subscriptions to avoid having to look up type of event via reflection
    /// </summary>
    /// <param name="resolvedEvent"></param>
    /// <returns></returns>
    public (object? Event, Type EventType) Deserialize(ResolvedEvent resolvedEvent)
    {
        if (resolvedEvent.Event == null)
        {
            return default;
        }

        var eventType = this.eventTypeMapper.MapFrom(resolvedEvent.Event.EventType ?? string.Empty);
        var @event = DeserializeEvent(resolvedEvent, eventType);

        return (Event: @event, EventType: eventType);
    }

    public byte[] Serialize(object obj)
    {
        // TODO: run benchmarks, probably not the fastest way to serialize
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, SerializerSettings));
    }

    private static object? DeserializeEvent(ResolvedEvent resolvedEvent, Type eventType)
    {
        return JsonConvert.DeserializeObject(
            Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span),
            eventType,
            SerializerSettings);
    }
}
