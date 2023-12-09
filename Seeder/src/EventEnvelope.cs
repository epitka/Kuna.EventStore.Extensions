using EventStore.Client;

namespace Kuna.EventStore.Seeder;

internal record struct EventEnvelope(string streamId, EventData EventData);
