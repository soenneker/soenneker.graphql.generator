using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Soenneker.Extensions.String;
using Soenneker.Extensions.ValueTask;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.GraphQL.Generator.Dtos;
namespace Soenneker.GraphQL.Generator.Cli;
public sealed class ConsoleHostedService : IHostedService
{
    private readonly ILogger<ConsoleHostedService> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IGraphQlGenerator _graphQlGenerator;
    private readonly CliOptions _cliOptions;

    private int? _exitCode;

    public ConsoleHostedService(ILogger<ConsoleHostedService> logger, IHostApplicationLifetime appLifetime,
        IGraphQlGenerator graphQlGenerator, CliOptions cliOptions)
    {
        _logger = logger;
        _appLifetime = appLifetime;
        _graphQlGenerator = graphQlGenerator;
        _cliOptions = cliOptions;
    }

    /// <summary>
    /// Starts the Console Hosted Service and begins its background work.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes after the Console Hosted Service has started.</returns>
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _appLifetime.ApplicationStarted.Register(() =>
        {
            Task.Run(async () =>
            {
                try
                {
                    if (!CliOptions.TryGetConfigPath(_cliOptions.Args, out string? configPath, out string? error, out bool showUsage, out int exitCode))
                    {
                        if (error.HasContent())
                            await Console.Error.WriteLineAsync(error);

                        if (showUsage)
                            Console.WriteLine(CliOptions.Usage);

                        _exitCode = exitCode;
                        return;
                    }

                    GenerationRunResult result = await _graphQlGenerator.RunFromConfig(configPath!, cancellationToken).NoSync();

                    Console.WriteLine($"Generated {result.Result.Files.Count} file(s) into '{result.OutputDirectory}'.");
                    Console.WriteLine($"Objects: {result.Result.ObjectCount}");
                    Console.WriteLine($"Inputs: {result.Result.InputCount}");
                    Console.WriteLine($"Enums: {result.Result.EnumCount}");
                    Console.WriteLine($"Interfaces: {result.Result.InterfaceCount}");
                    Console.WriteLine($"Unions: {result.Result.UnionCount}");
                    Console.WriteLine($"Scalars: {result.Result.ScalarCount}");
                    Console.WriteLine($"Operation files: {result.Result.OperationFileCount}");

                    _exitCode = 0;
                }
                catch (Exception e)
                {
                    if (Debugger.IsAttached)
                        Debugger.Break();

                    _logger.LogError(e, "Unhandled exception");
                    _exitCode = 1;
                }
                finally
                {
                    _appLifetime.StopApplication();
                }
            }, cancellationToken);
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the Console Hosted Service and waits for its background work to finish.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes after the Console Hosted Service has stopped.</returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Exiting with return code: {exitCode}", _exitCode);
        Environment.ExitCode = _exitCode.GetValueOrDefault(-1);
        return Task.CompletedTask;
    }
}
