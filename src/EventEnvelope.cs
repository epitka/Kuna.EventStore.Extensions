using EventStore.Client;

namespace Kuna.EventStore.Seeder;

public record struct EventEnvelope(string streamId, EventData EventData);
