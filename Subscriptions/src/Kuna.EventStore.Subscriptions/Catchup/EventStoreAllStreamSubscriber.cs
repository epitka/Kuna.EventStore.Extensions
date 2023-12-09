using EventTypeFilter = EventStore.Client.EventTypeFilter;

namespace Kuna.EventStore.Subscriptions.Catchup;


/// <summary>
/// generic source that subscribes to EventStore $all stream and pushes events into outputQueue after transformation using eventTransformer
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="subscriptionSettings"></param>
/// <param name="client"></param>
/// <param name="eventTransformer"></param>
/// <param name="logger"></param>
public abstract class EventStoreSubscriber(
        EventStoreSubscriptionSettings subscriptionSettings,
        EventStoreClient client,
        ILogger<EventStoreSubscriber> logger)
{
    private StreamSubscription? Subscription { get; set; }

    protected int droppedSubscriptionCount;
    protected Position currentCheckPoint;
    protected CancellationTokenSource? cts = default!;

    protected abstract Task OnEventAppeared(
            StreamSubscription streamSubscription,
            ResolvedEvent resolvedEvent,
            CancellationToken ct);

    /// <summary>
    ///  Starts the subscription to EventStore $all stream
    /// </summary>
    /// <param name="checkpoint">postion to start processing events from</param>
    /// <param name="outputQueue">FIFO Queue into which transformed events from EventStore will be pushed</param>
    /// <param name="eventTransformerDelegate">delegate that performs transformation of the ResolvedEvent into TOut type</param>
    /// <param name="ct"></param>
    public async Task Start(
    CheckPoint? checkpoint,
    CancellationToken ct)
    {
        this.cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var startFrom = Position.Start;

        if (checkpoint is not null)
        {
            startFrom = new Position(checkpoint.CommitPosition, checkpoint.PreparePosition);
        }

        await this.InternalStart(startFrom, this.cts.Token);

        this.droppedSubscriptionCount = 0;

        try
        {
            // Wait indefinitely until the cancellation token is triggered
            await Task.Delay(System.Threading.Timeout.Infinite, ct);
        }
        catch (OperationCanceledException)
        {
            // This block is executed when the cancellation token is triggered
            logger.LogWarning("Stopped ResolvedEventSource at {Now}", DateTime.Now);
        }
    }

    private async Task InternalStart(Position checkpoint, CancellationToken ct)
    {
        this.currentCheckPoint = checkpoint;

        await this.Subscribe(checkpoint, ct);
    }

    public void Stop()
    {
        logger.LogWarning("Stopping EventStoreSource");

        this.Subscription?.Dispose();

        this.cts!.Cancel();

        logger.LogWarning("Stopped, received {Count} events from EventStore", this.cnt);
    }

    private async Task Subscribe(
        Position checkPoint,
        CancellationToken ct)
    {
        var filter = subscriptionSettings.Filter;

        var filterOptions = filter switch
        {
            null => new SubscriptionFilterOptions(EventTypeFilter.ExcludeSystemEvents()),
            { Type: "EventType", Expression: var expr } when expr.StartsWith("prefix:") => new SubscriptionFilterOptions(EventTypeFilter.Prefix(expr[7..])),
            { Type: "EventType", Expression: var expr } when expr.StartsWith("regex:") => new SubscriptionFilterOptions(EventTypeFilter.RegularExpression(expr[6..])),
            { Type: "StreamName", Expression: var expr } when expr.StartsWith("prefix:") => new SubscriptionFilterOptions(StreamFilter.Prefix(expr[7..])),
            { Type: "StreamName", Expression: var expr } when expr.StartsWith("regex:") => new SubscriptionFilterOptions(StreamFilter.RegularExpression(expr[6..])),
            _ => throw new InvalidOperationException($"Invalid filter type {filter.Type} or expression {filter.Expression}")
        };

        this.Subscription = await client!
            .SubscribeToAllAsync(
                eventAppeared: this.OnEventAppeared,
                filterOptions: filterOptions,
                start: FromAll.After(checkPoint),
                subscriptionDropped: this.OnSubscriptionDropped,
                resolveLinkTos: false,
                cancellationToken: ct);

        logger.LogInformation(
            "Created subscription to stream $all at {Now}",
            DateTime.Now);
    }

    private readonly long cnt = 0;

    private void OnSubscriptionDropped(
        StreamSubscription catchUpSubscription,
        SubscriptionDroppedReason reason,
        Exception? exception)
    {
        logger.LogWarning(
            "All Stream subscription has been dropped at {Now} for Reason: {@Reason}",
            DateTime.Now,
            reason);

        this.droppedSubscriptionCount++;

        if (this.droppedSubscriptionCount > subscriptionSettings.MaxConnectionRetries)
        {
            logger.LogError(
                "All Stream subscription has been dropped at {Now} for Reason: {@Reason} and MaxConnectionRetries has been reached. Stopping...",
                DateTime.UtcNow.ToString(),
                reason);

            this.Stop();
        }

        _ = this.AttemptRestart(reason, this.cts!.Token).Wait(TimeSpan.FromSeconds(3));
    }

    private async Task AttemptRestart(SubscriptionDroppedReason reason, CancellationToken ct)
    {
        if (reason == SubscriptionDroppedReason.ServerError)
        {
            await this.InternalStart(this.currentCheckPoint, ct);
        }
    }
}
