using Kuna.EventStore.Seeder;

namespace SeederExample;

public class EventsGeneratorFactory : IEventGeneratorFactory
{
    public IEventsGenerator GetEventsGenerator()
    {
        return new EventsGenerator();
    }
}
