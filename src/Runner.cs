using System.Collections.Concurrent;
using EventStore.Client;
using Kuna.EventStore.Seeder.Services;
using Microsoft.Extensions.Logging;

namespace Kuna.EventStore.Seeder;

public class Runner
{
    private readonly Func<IEventsGenerator> eventsGeneratorFunc;
    private readonly IEventDataFactory eventDataFactory;
    private readonly EventStoreClient client;
    private readonly ILogger<Runner> logger;

    public Runner(
        Func<IEventsGenerator> eventsGeneratorFunc,
        IEventDataFactory eventDataFactory,
        EventStoreClient client,
        ILogger<Runner> logger)
    {
        this.eventsGeneratorFunc = eventsGeneratorFunc;
        this.eventDataFactory = eventDataFactory;
        this.client = client;
        this.logger = logger;
    }

    public async Task Start(
        RunOptions runOptions,
        CancellationToken ct)
    {
        var streamsPerWorker = runOptions.NumberOfStreams / runOptions.NumberOfWorkers;

        var tasks = new List<Task>();

        var statsMap = new ConcurrentDictionary<Guid, Stats>();

        var progress = new Progress<Stats>();

        progress.ProgressChanged += (sender, stats) =>
        {
            if (statsMap.ContainsKey(stats.StatsId))
            {
                statsMap[stats.StatsId] = stats;
            }
            else
            {
                while (!statsMap.TryAdd(stats.StatsId, stats))
                {
                    Console.WriteLine("oops");
                }
            }
        };

        var statsTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        _ = Task.Run(
            async () =>
            {
                while (await statsTimer.WaitForNextTickAsync(ct))
                {
                    var totals = new Stats(Guid.NewGuid());

                    foreach (var entry in statsMap)
                    {
                        var s = entry.Value;

                        totals.TotalEventsWritten += s.TotalEventsWritten;
                        totals.TotalEventsGenerated += s.TotalEventsGenerated;
                        totals.TotalStreamsGenerated += s.TotalStreamsGenerated;
                        totals.TotalStreamsStarted += s.TotalStreamsStarted;
                    }

                    Console.WriteLine($"{totals.TotalEventsGenerated} events generated for {totals.TotalStreamsGenerated} streams; written: {totals.TotalEventsWritten}");
                }
            },
            ct);

        // in case of fractions add diff to first stream
        var diff = runOptions.NumberOfStreams - runOptions.NumberOfWorkers * streamsPerWorker;

        for (var i = 0; i < runOptions.NumberOfWorkers; i++)
        {
            if (i != 0)
            {
                diff = 0;
            }

            var worker = new Worker(
                this.eventDataFactory,
                this.client,
                this.eventsGeneratorFunc.Invoke(),
                new WorkerOptions(streamsPerWorker + diff));

            var t = worker.Run(progress, ct);

            tasks.Add(t);
        }

        await Task.WhenAll(tasks);
    }
}
