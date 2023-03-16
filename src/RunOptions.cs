namespace Kuna.EventStore.Seeder;

public record RunOptions(
    int NumberOfStreams,
    int NumberOfWorkers);
