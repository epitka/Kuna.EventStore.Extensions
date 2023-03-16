namespace Kuna.EventStore.Seeder;

public class Stats
{
    public Stats(Guid id)
    {
        this.StatsId = id;
    }

    public Guid StatsId { get; }
    public int TotalEventsGenerated { get; set; }

    public int TotalStreamsStarted { get; set; }

    public int TotalStreamsGenerated { get; set; }

    public int TotalEventsWritten { get; set; }
}
