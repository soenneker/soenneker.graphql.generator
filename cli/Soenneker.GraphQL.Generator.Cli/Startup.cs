using Microsoft.Extensions.DependencyInjection;
using Soenneker.GraphQL.Generator.Registrars;

namespace Soenneker.GraphQL.Generator.Cli;

/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, string[] args)
    {
        services.SetupIoC(args);
    }

    public static IServiceCollection SetupIoC(this IServiceCollection services, string[] args)
    {
        services.AddSingleton(new CliOptions(args));
        services.AddHostedService<ConsoleHostedService>();
        services.AddGraphQlGeneratorAsScoped();

        return services;
    }
}
