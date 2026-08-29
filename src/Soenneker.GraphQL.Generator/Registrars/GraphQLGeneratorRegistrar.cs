using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.GraphQL.Generator.Registrars;

/// <summary>
/// Registers GraphQL generator services and required file system utilities.
/// </summary>
public static class GraphQlGeneratorRegistrar
{
    /// <summary>
    /// Adds GraphQL generator services as scoped dependencies.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddGraphQlGeneratorAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped();
        services.AddDirectoryUtilAsScoped();
        services.TryAddScoped<IGraphQlGenerator, GraphQlGenerator>();

        return services;
    }

    /// <summary>
    /// Adds GraphQL generator services as singleton dependencies.
    /// </summary>
    /// <param name="services">Service collection that receives the registration.</param>
    /// <returns>The same service collection, so additional registrations can be chained.</returns>
    public static IServiceCollection AddGraphQlGeneratorAsSingleton(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton();
        services.AddDirectoryUtilAsSingleton();
        services.TryAddSingleton<IGraphQlGenerator, GraphQlGenerator>();

        return services;
    }
}
