namespace Kuna.EventStore.Seeder;

public interface IEventGeneratorFactory
{
    public IEventsGenerator GetEventsGenerator();
}
