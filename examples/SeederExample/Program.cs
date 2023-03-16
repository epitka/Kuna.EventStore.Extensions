using System.Reflection;
using Kuna.EventStore.Seeder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeederExample;
using SeederExample.Events;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};

Type[] EventsDiscoveryFunc(Assembly[] assemblies)
{
    var interfaceType = typeof(IAggregateEvent);
    var eventTypes = assemblies.SelectMany(i => i.GetTypes())
                               .Where(x => interfaceType.IsAssignableFrom(x))
                               .ToArray();

    return eventTypes;
}

IEventsGenerator EventsGeneratorFactory()
{
    return new EventsGenerator();
}

var h = Host.CreateDefaultBuilder();

var cfg = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

h.ConfigureServices(
    (c, s) =>
    {
        s.AddEventStoreSeeder(
            cfg,
            new[] { typeof(Program).Assembly },
            EventsDiscoveryFunc,
            EventsGeneratorFactory);

        s.AddTransient<IEventsGenerator, EventsGenerator>();
    });

var a = h.Build();

await a.StartAsync(cts.Token);
// read arguments

var runOptions = new RunOptions(
    10_000,
    50);

var runner = a.Services.GetRequiredService<Runner>();

await runner.Start(runOptions, cts.Token);

await a.StopAsync(cts.Token);

// bootstrap DI



Console.WriteLine("Process finished ");
