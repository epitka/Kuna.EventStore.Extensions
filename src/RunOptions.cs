using CommandLine;

namespace Kuna.EventStore.Seeder;


public class RunOptions
{
    [Option('s', "streams", Required = false, HelpText = "Number of disctinct streams to write; default:100,000")]
    public int NumberOfStreams { get; set; } = 100_000;

    [Option('w', "workers", Required = false, HelpText = "Number of workers to write to event store, default:50")]
    public int NumberOfWorkers { get; set; } = 50;

}
