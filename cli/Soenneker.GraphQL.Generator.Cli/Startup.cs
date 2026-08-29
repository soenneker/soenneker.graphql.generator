using Microsoft.Extensions.DependencyInjection;
using Soenneker.GraphQL.Generator.Registrars;
namespace Soenneker.GraphQL.Generator.Cli;
/// <summary>
/// Console type startup
/// </summary>
public static class Startup
{
    /// <summary>
    /// Registers the services required by the application host.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <param name="args">Command-line arguments passed to the application.</param>
    public static void ConfigureServices(IServiceCollection services, string[] args)
    {
        services.SetupIoC(args);
    }

    /// <summary>
    /// Registers the services required by the application.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <param name="args">Command-line arguments passed to the application.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection SetupIoC(this IServiceCollection services, string[] args)
    {
        services.AddSingleton(new CliOptions(args));
        services.AddHostedService<ConsoleHostedService>();
        services.AddGraphQlGeneratorAsSingleton();

        return services;
    }
}
