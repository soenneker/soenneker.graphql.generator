using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
namespace Soenneker.GraphQL.Generator.Cli;

public static class Program
{
    private static CancellationTokenSource? _cts;

    /// <summary>
    /// Runs the application using the supplied command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A task that completes when the application exits.</returns>
    public static async Task Main(string[] args)
    {
        _cts = new CancellationTokenSource();
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            await CreateHostBuilder(args).RunConsoleAsync(_cts.Token);
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
            _cts.Dispose();
        }
    }

    /// <summary>
    /// Used for WebApplicationFactory, cannot delete, cannot change access, cannot change number of parameters.
    /// </summary>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>A host builder configured with the application services and settings.</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        IHostBuilder host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, builder) =>
            {
                builder.AddEnvironmentVariables();
                builder.SetBasePath(hostingContext.HostingEnvironment.ContentRootPath);

                builder.Build();
            })
            .ConfigureServices((_, services) => { Startup.ConfigureServices(services, args); });

        return host;
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        _cts?.Cancel();
    }
}
