using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.Utils.Directory.Registrars;
using Soenneker.Utils.File.Registrars;

namespace Soenneker.GraphQL.Generator.Registrars;

/// <summary>
/// Registers GraphQL generator services and required file system utilities.
/// </summary>
public static class GraphQLGeneratorRegistrar
{
    /// <summary>
    /// Adds GraphQL generator services as scoped dependencies.
    /// </summary>
    public static IServiceCollection AddGraphQLGeneratorAsScoped(this IServiceCollection services)
    {
        services.AddFileUtilAsScoped();
        services.AddDirectoryUtilAsScoped();
        services.TryAddScoped<IGraphQLGenerator, GraphQLGenerator>();

        return services;
    }

    /// <summary>
    /// Adds GraphQL generator services as singleton dependencies.
    /// </summary>
    public static IServiceCollection AddGraphQLGeneratorAsSingleton(this IServiceCollection services)
    {
        services.AddFileUtilAsSingleton();
        services.AddDirectoryUtilAsSingleton();
        services.TryAddSingleton<IGraphQLGenerator, GraphQLGenerator>();

        return services;
    }
}
