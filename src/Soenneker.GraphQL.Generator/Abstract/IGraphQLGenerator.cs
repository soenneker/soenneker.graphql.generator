using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Dtos;

namespace Soenneker.GraphQL.Generator.Abstract;

/// <summary>
/// Generates C# types and optional JsonSerializerContext from GraphQL SDL schemas.
/// </summary>
public interface IGraphQlGenerator
{
    /// <summary>
    /// Parses the given GraphQL schema and returns generated C# files and counts.
    /// </summary>
    /// <param name="schemaContent">Full GraphQL SDL schema text.</param>
    /// <param name="config">Generation options (namespace, output options, scalar mappings, etc.).</param>
    /// <returns>Generated files and type counts. Caller is responsible for writing files to disk.</returns>
    /// <exception cref="Exception">When the schema is invalid or parsing fails.</exception>
    GenerationResult Generate(string schemaContent, GeneratorConfig config);

    /// <summary>
    /// Parses the given GraphQL schema, generates source files, and writes them to the configured output directory.
    /// </summary>
    /// <param name="schemaContent">Full GraphQL SDL schema text.</param>
    /// <param name="config">Generation options. <see cref="GeneratorConfig.OutputDirectory"/> is required.</param>
    /// <param name="cancellationToken">A token used to cancel the generation run.</param>
    /// <returns>The generation summary and resolved output directory. Existing generated paths are overwritten; unrelated and stale files are retained.</returns>
    ValueTask<GenerationRunResult> Run(string schemaContent, GeneratorConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads configuration and schema from disk, generates source files, and writes them to the configured output directory.
    /// </summary>
    /// <param name="configPath">Path to the generator config JSON file.</param>
    /// <param name="cancellationToken">A token used to cancel the generation run.</param>
    /// <returns>The generation summary and resolved output directory. Relative schema and output paths are resolved from the process working directory.</returns>
    ValueTask<GenerationRunResult> RunFromConfig(string configPath, CancellationToken cancellationToken = default);
}
