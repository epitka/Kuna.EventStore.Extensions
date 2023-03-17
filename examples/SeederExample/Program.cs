using System.Reflection;
using Kuna.EventStore.Seeder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SeederExample;
using SeederExample.Events;

var cts = new CancellationTokenSource();
var ct = cts.Token;
Console.CancelKeyPress += (s, e) =>
{
    Console.WriteLine("Canceling...");
    cts.Cancel();
    e.Cancel = true;
};

var host = Host.CreateDefaultBuilder();

var cfg = new ConfigurationBuilder()
          .AddJsonFile("appsettings.json")
          .Build();

// provide a func to discover events definitions. It can be as in this example by marker interface.
Type[] EventsDiscoveryFunc(Assembly[] assemblies)
{
    var interfaceType = typeof(IAggregateEvent);

    var eventTypes = assemblies.SelectMany(i => i.GetTypes())
                               .Where(x => interfaceType.IsAssignableFrom(x))
                               .ToArray();

    return eventTypes;
}

host.ConfigureServices(
    (c, s) =>
    {
        // wire up Seeder services
        s.AddEventStoreSeeder(
            cfg,
            new[] { typeof(Program).Assembly },
            EventsDiscoveryFunc);

        // wire up your implementation of the events generator and factory
        s.AddSingleton<IEventGeneratorFactory, EventsGeneratorFactory>();
    });

var a = host.Build();

await a.StartAsync(ct);

// read arguments

await Seeder.Run(args, a.Services, ct);

await a.StopAsync(cts.Token);

// bootstrap DI

Console.WriteLine("Process finished ");
