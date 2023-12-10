
// Simple example of resolved events consumer
// Uses serilog as a sink for events. It writes events to console and file
// Events are read from the event store from the beginning, but one could start from a specific checkpoint or
// from the end (current), observing new events as they are written to the event store
// Sink could be anything else, for example a database or a message queue
// Akka.NET streams could be used to create complex event processing pipelines

using System.Diagnostics;
using System.Text;

var cts = new CancellationTokenSource();
var ct = cts.Token;
Console.CancelKeyPress += (s, e) =>
{
    Log.Information("Canceling...");
    cts.Cancel();
    e.Cancel = false;
};

var host = Host.CreateDefaultBuilder();

var cfg = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

host.ConfigureServices(
    (c, s) =>
    {
        // wire up Subscriptions services
        s.AddResolvedEventsSource(cfg);
    });

// create logger to console and file
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log.txt"),
        rollingInterval: RollingInterval.Minute,
        rollOnFileSizeLimit: true,
        fileSizeLimitBytes: 1_000_000, // 1 MB
        retainedFileCountLimit: 1)
    .CreateLogger();

try
{
    var a = host.Build();

    await a.StartAsync(ct);

    var resolvedEventsSource = a.Services.GetRequiredService<IResolvedEventSource>();

    // start from the beginning
    // TODO: add a way to start from a specific checkpoint or from the end (current)
    // start resolved events source
    var sourceTask = resolvedEventsSource.Start(new CheckPoint(0, 0), ct);

    var i = 0;

    var stopwatch = Stopwatch.StartNew();
    stopwatch.Start();
    // iterate over resolved events
    await foreach (var resolvedEvent in resolvedEventsSource.ResolvedEvents(ct))
    {
        i++;

        if (ct.IsCancellationRequested)
        {
            Log.Information("Processed {Count} events in {Elapsed} ms",
                i,
                stopwatch.ElapsedMilliseconds,
                i/stopwatch.ElapsedMilliseconds);

            await Task.Yield();

            break;
        }

        Log.Information("{i}, {EventStreamId}, {Data}",
            i,
            resolvedEvent.Event.EventStreamId,
           (Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray())));
    }  

    await a.StopAsync(ct);
}
catch (Exception ex)
{
    Log.Error(ex, "Something went wrong");
}
finally
{
    Log.CloseAndFlush();
}