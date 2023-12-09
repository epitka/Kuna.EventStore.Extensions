
using CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Kuna.EventStore.Seeder;

public static class Seeder
{
    public static async Task Run(
        string[] args,
        IServiceProvider sp,
        CancellationToken ct
        )
    {
        var runOptions = Parser.Default.ParseArguments<RunOptions>(args).Value;

        using var scope = sp.CreateScope();

        var runner = scope.ServiceProvider.GetRequiredService<Runner>();

        await runner.Start(runOptions, ct).ConfigureAwait(false);
    }
}

