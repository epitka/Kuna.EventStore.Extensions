using EventStore.Client;

namespace Kuna.EventStore.Seeder;

/// <summary>
///     Implement this interface and return collection of events in order in one or more streams
/// </summary>
public interface IEventsGenerator
{
    /// <summary>
    ///
    /// </summary>
    /// <returns>ValueTuple where:
    /// StreamPrefix is prefix of the stream name with a "-" like "order-"
    /// Event is instance of event
    /// AggregateId is string value of the aggregate to which event belongs
    /// </returns>
    IEnumerable<(string StreamPrefix, object Event, string AggregateId)> Events();
}
