namespace Kuna.EventStore.Subscriptions.Catchup;

public interface IResolvedEventSource
{
    Task Start(CheckPoint? checkpoint, CancellationToken ct);

    void Stop();

    IAsyncEnumerable<ResolvedEvent> ResolvedEvents { get; }
}

public class ResolvedEventSource(
    EventStoreSubscriptionSettings subscriptionSettings,
    EventStoreClient client,
    ILogger<ResolvedEventSource> logger)
    : EventStoreSubscriber(subscriptionSettings, client, logger), IResolvedEventSource
{

    // TODO: make this configurable
    private readonly Channel<ResolvedEvent> channel = Channel.CreateBounded<ResolvedEvent>(10_000);

    protected override async Task OnEventAppeared(
     StreamSubscription streamSubscription,
     ResolvedEvent resolvedEvent,
     CancellationToken ct)
    {
        // skip system events
        if (resolvedEvent.OriginalStreamId.StartsWith('$'))
        {
            return;
        }

        await this.channel.Writer!.WriteAsync(resolvedEvent, ct);
    }

    public IAsyncEnumerable<ResolvedEvent> ResolvedEvents => this.channel.Reader.ReadAllAsync();

}

