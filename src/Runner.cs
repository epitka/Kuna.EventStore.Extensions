using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Kuna.EventStore.Seeder;

internal class Runner
{
    private readonly IWorkerFactory workerFactory;
    private readonly ILogger<Runner> logger;

    public Runner(
        IWorkerFactory workerFactory,
        ILogger<Runner> logger)
    {
        this.workerFactory = workerFactory;
        this.logger = logger;
    }

    public async Task Start(
        RunOptions runOptions,
        CancellationToken ct)
    {
        var progress = SetUpProgress(ct);

        var streamsPerWorker = runOptions.NumberOfStreams / runOptions.NumberOfWorkers;

        var tasks = new List<Task>();

        // in case of fractions add diff to first stream
        var diff = runOptions.NumberOfStreams - runOptions.NumberOfWorkers * streamsPerWorker;

        for (var i = 0; i < runOptions.NumberOfWorkers; i++)
        {
            if (i != 0)
            {
                diff = 0;
            }

            var worker = this.workerFactory.GetWorker(streamsPerWorker + diff);

            var t = worker.Run(progress, ct);

            tasks.Add(t);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private static Progress<Stats> SetUpProgress(CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

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
                }
            }
        };

        var statsTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));

        _ = Task.Run(
            async () =>
            {
                while (await statsTimer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    if (cts.IsCancellationRequested)
                    {
                        break;
                    }

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

                    if (totals.TotalEventsWritten == totals.TotalEventsGenerated)
                    {
                        Console.WriteLine("All events written to EventStore");
                        cts.Cancel();
                    }
                }
            },
            cts.Token);

        return progress;
    }
}
