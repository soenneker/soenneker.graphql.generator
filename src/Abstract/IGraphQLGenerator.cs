using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Models;

namespace Soenneker.GraphQL.Generator.Abstract;

/// <summary>
/// Generates C# types and optional JsonSerializerContext from GraphQL SDL schemas.
/// </summary>
public interface IGraphQLGenerator
{
    /// <summary>
    /// Parses the given GraphQL schema and returns generated C# files and counts.
    /// </summary>
    /// <param name="schemaContent">Full GraphQL SDL schema text.</param>
    /// <param name="config">Generation options (namespace, output options, scalar mappings, etc.).</param>
    /// <returns>Generated files and type counts. Caller is responsible for writing files to disk.</returns>
    /// <exception cref="Exception">When the schema is invalid or parsing fails.</exception>
    GenerationResult Generate(string schemaContent, GeneratorConfig config);
}
