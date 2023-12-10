namespace Kuna.EventStore.Subscriptions.Catchup;

public interface IResolvedEventSource
{
    Task Start(CheckPoint? checkpoint, CancellationToken ct);

    void Stop();

    IAsyncEnumerable<ResolvedEvent> ResolvedEvents(CancellationToken ct);
}

public class ResolvedEventSource(
    EventStoreSubscriptionSettings subscriptionSettings,
    EventStoreClient client,
    ILogger<ResolvedEventSource> logger)
    : EventStoreSubscriber(subscriptionSettings, client, logger), IResolvedEventSource
{

    // TODO: make this configurable
    private readonly Channel<ResolvedEvent> channel = Channel.CreateBounded<ResolvedEvent>(100_000);

    protected override async Task OnEventAppeared(
     StreamSubscription streamSubscription,
     ResolvedEvent resolvedEvent,
     CancellationToken ct)
    {
        await this.channel.Writer!.WriteAsync(resolvedEvent, ct);
    }

    public IAsyncEnumerable<ResolvedEvent> ResolvedEvents(CancellationToken ct=default) => this.channel.Reader.ReadAllAsync(ct);

}

