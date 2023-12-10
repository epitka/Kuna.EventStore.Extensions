using System.Threading.Channels;
using EventStore.Client;
using Kuna.EventStore.Seeder.Services;

namespace Kuna.EventStore.Seeder;

internal class Worker
{
    private static readonly Random Randomizer = Random.Shared;

    private readonly Channel<EventEnvelope> channel;
    private readonly EventStoreClient client;
    private readonly IEventDataFactory eventDataFactory;
    private readonly IEventsGenerator generator;

    private readonly object lockSource = new();

    private readonly Stats stats;
    private readonly WorkerOptions workerOptions;

    private int completedCnt;
    private int startedCnt;

    private int totalEvents;
    private int totalWritten;

    private readonly Guid workerId = Guid.NewGuid();

    public Worker(
        IEventDataFactory eventDataFactory,
        EventStoreClient client,
        IEventsGenerator generator,
        WorkerOptions workerOptions)
    {
        this.eventDataFactory = eventDataFactory;
        this.client = client;
        this.generator = generator;
        this.workerOptions = workerOptions;

        this.channel = Channel.CreateBounded<EventEnvelope>(this.workerOptions.NumberOfStreams * 100);
        this.stats = new Stats(this.workerId);
    }

    public async Task Run(IProgress<Stats> progress, CancellationToken cancellationToken)
    {
        this.StartStatistics(progress, cancellationToken);

        var t = this.StartSink(cancellationToken);

        // 100 tasks to get some randomness of entries into a channel
        for (var i = 0; i < 100; i++)
        {
            this.StartSource(this.workerOptions, cancellationToken);
        }

        await t.ConfigureAwait(false);

        progress.Report(this.stats);

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
    }

    private async Task StartSink(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        this.totalWritten = 0;
        var ed = new EventData[1];

        while (await this.channel.Reader.WaitToReadAsync(cts.Token).ConfigureAwait(false))
        {
            while (this.channel.Reader.TryRead(out var envelope))
            {
                ed[0] = envelope.EventData;

                await this.client
                          .AppendToStreamAsync(envelope.streamId, StreamState.Any, ed, cancellationToken: cts.Token)
                          .ConfigureAwait(false);

                this.totalWritten++;
            }
        }
    }

    private void StartSource(
        WorkerOptions workerOptions,
        CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var ct = cts.Token;

        var done = false;
        Task.Run(
            async () =>
            {
                while (!done)
                {
                    if (cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    lock (this.lockSource)
                    {
                        if (this.startedCnt >= workerOptions.NumberOfStreams)
                        {
                            done = true;
                            break;
                        }

                        this.startedCnt++;
                    }

                    var entries = this.generator.Events();

                    foreach (var entry in entries)
                    {
                        var (streamPrefix, @event, aggregateId) = entry;

                        var streamId = streamPrefix + aggregateId;

                        var eventData = this.eventDataFactory.From(@event);

                        var envelope = new EventEnvelope(streamId, eventData);

                        await Task.Delay(Randomizer.Next(10, 50), ct).ConfigureAwait(false);

                        while (!this.channel.Writer.TryWrite(envelope))
                        {
                            await Task.Delay(Randomizer.Next(10, 50), ct).ConfigureAwait(false);
                        }

                        Interlocked.Increment(ref this.totalEvents);
                    }

                    lock (this.lockSource)
                    {
                        this.completedCnt++;

                        if (this.completedCnt >= this.workerOptions.NumberOfStreams)
                        {
                            var completed = this.channel.Writer.TryComplete();

                            if (completed)
                            {
                                cts.Cancel();
                            }
                        }
                    }
                }
            },
            cts.Token);
    }

    private void StartStatistics(IProgress<Stats> progress, CancellationToken ct)
    {
        Task.Run(
            async () =>
            {
                var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    this.stats.TotalEventsGenerated = this.totalEvents;
                    this.stats.TotalEventsWritten = this.totalWritten;
                    this.stats.TotalStreamsGenerated = this.completedCnt;
                    this.stats.TotalStreamsStarted = this.startedCnt;

                    progress.Report(this.stats);
                }
            },
            ct);
    }
}
