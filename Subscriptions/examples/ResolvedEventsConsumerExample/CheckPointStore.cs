using Kuna.EventStore.Subscriptions.Catchup;

namespace ResolvedEventsConsumerExample
{
    // dummy implementation, always starts from the beginning of the stream
    internal class CheckPointStore : ICheckPointStore
    {
        public Task<CheckPoint> GetCheckPoint(CancellationToken ct)
        {
            return Task.FromResult(new CheckPoint(0, 0));
        }
    }
}
