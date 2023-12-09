using EventStore.Client;

namespace Kuna.EventStore.Seeder.Services;

public interface IEventDataFactory
{
    public EventData From(object obj);
}

public class EventDataFactory : IEventDataFactory
{
    private readonly IEventMetadataFactory metadataFactory;
    private readonly IEventStoreSerializer storeSerializer;

    public EventDataFactory(
        IEventStoreSerializer storeSerializer,
        IEventMetadataFactory metadataFactory)
    {
        this.storeSerializer = storeSerializer;
        this.metadataFactory = metadataFactory;
    }

    public EventData From(object obj)
    {
        var eventId = Uuid.NewUuid();

        try
        {
            // TODO: can we avoid constant reflection here ???
            // if we had static field on each event with a name of the event we could
            // forgo reflection here
            var eType = obj.GetType();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        // TODO: can we avoid constant reflection here ???
        // if we had static field on each event with a name of the event we could
        // forgo reflection here
        var eventType = obj.GetType();
        var data = this.storeSerializer.Serialize(obj);
        var metadata = this.storeSerializer.Serialize(this.metadataFactory.Get());
        return new EventData(eventId, eventType.Name, data, metadata);
    }
}
