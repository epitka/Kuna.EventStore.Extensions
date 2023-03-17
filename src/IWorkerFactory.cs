using EventStore.Client;
using Kuna.EventStore.Seeder.Services;

namespace Kuna.EventStore.Seeder;

public interface IWorkerFactory
{
    Worker GetWorker(int streamsCnt);
}

public class WorkerFactory : IWorkerFactory
{
    private readonly EventStoreClient client;
    private readonly IEventGeneratorFactory generatorFactory;
    private readonly IEventDataFactory eventDataFactory;

    public WorkerFactory(
        EventStoreClient client,
        IEventGeneratorFactory generatorFactory,
        IEventDataFactory eventDataFactory
        )
    {
        this.client = client;
        this.generatorFactory = generatorFactory;
        this.eventDataFactory = eventDataFactory;
    }

    public Worker GetWorker(int streamsCnt)
    {
        var worker = new Worker(
            this.eventDataFactory,
            this.client,
            this.generatorFactory.GetEventsGenerator(),
            new WorkerOptions(streamsCnt));

        return worker;
    }
}

