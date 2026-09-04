using System.Text.Json;
using System.Text.Json.Serialization;
using GraphQLParser;
using GraphQLParser.AST;
using Soenneker.Extensions.Task;
using Soenneker.Extensions.ValueTask;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.GraphQL.Generator.Generators;
using Soenneker.Utils.Directory.Abstract;
using Soenneker.Utils.File.Abstract;
using Soenneker.Extensions.String;

namespace Soenneker.GraphQL.Generator;

/// <inheritdoc cref="IGraphQlGenerator" />
public sealed class GraphQlGenerator : IGraphQlGenerator
{
    private readonly IFileUtil? _fileUtil;
    private readonly IDirectoryUtil? _directoryUtil;

    public GraphQlGenerator()
    {
    }

    public GraphQlGenerator(IFileUtil fileUtil, IDirectoryUtil directoryUtil)
    {
        _fileUtil = fileUtil;
        _directoryUtil = directoryUtil;
    }

    public GenerationResult Generate(string schemaContent, GeneratorConfig config)
    {
        GraphQLDocument document = Parser.Parse(schemaContent);
        var schemaGenerator = new SchemaGenerator(config, document);
        return schemaGenerator.Generate(document);
    }

    public async ValueTask<GenerationRunResult> Run(string schemaContent, GeneratorConfig config, CancellationToken cancellationToken = default)
    {
        if (_fileUtil is null || _directoryUtil is null)
            throw new InvalidOperationException($"{nameof(GraphQlGenerator)} requires {nameof(IFileUtil)} and {nameof(IDirectoryUtil)} for file-writing generation.");

        if (string.IsNullOrWhiteSpace(config.OutputDirectory))
            throw new InvalidOperationException("OutputDirectory is required in config.");

        GenerationResult result = Generate(schemaContent, config);
        string resolvedOutputDirectory = Path.GetFullPath(config.OutputDirectory);

        await WriteFiles(result, resolvedOutputDirectory, cancellationToken).NoSync();

        return new GenerationRunResult(resolvedOutputDirectory, result);
    }

    public async ValueTask<GenerationRunResult> RunFromConfig(string configPath, CancellationToken cancellationToken = default)
    {
        if (_fileUtil is null || _directoryUtil is null)
            throw new InvalidOperationException($"{nameof(GraphQlGenerator)} requires {nameof(IFileUtil)} and {nameof(IDirectoryUtil)} for config-based generation.");

        if (string.IsNullOrWhiteSpace(configPath))
            throw new ArgumentException("Config path is required.", nameof(configPath));

        string resolvedConfigPath = Path.GetFullPath(configPath);

        if (!await _fileUtil.Exists(resolvedConfigPath, cancellationToken).NoSync())
            throw new FileNotFoundException($"Config file not found: {resolvedConfigPath}", resolvedConfigPath);

        string configJson = await _fileUtil.Read(resolvedConfigPath, cancellationToken: cancellationToken).NoSync();

        GeneratorConfig config = JsonSerializer.Deserialize<GeneratorConfig>(configJson, JsonOptions.Default)
                                 ?? throw new InvalidOperationException("Failed to deserialize config.");

        if (string.IsNullOrWhiteSpace(config.SchemaPath))
            throw new InvalidOperationException("SchemaPath is required in config.");

        string resolvedSchemaPath = Path.GetFullPath(config.SchemaPath);

        if (!await _fileUtil.Exists(resolvedSchemaPath, cancellationToken).NoSync())
            throw new FileNotFoundException($"Schema file not found: {resolvedSchemaPath}", resolvedSchemaPath);

        if (config.OutputDirectory.IsNullOrWhiteSpace())
            throw new InvalidOperationException("OutputDirectory is required in config.");

        string schemaText = await _fileUtil.Read(resolvedSchemaPath, log: false, cancellationToken: cancellationToken).NoSync();
        return await Run(schemaText, config, cancellationToken).NoSync();
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private async ValueTask WriteFiles(GenerationResult result, string resolvedOutputDirectory, CancellationToken cancellationToken)
    {
        await _directoryUtil!.Create(resolvedOutputDirectory, log: false, cancellationToken: cancellationToken).NoSync();

        foreach (GeneratedFile file in result.Files)
        {
            string fullPath = Path.Combine(resolvedOutputDirectory, file.RelativePath);
            string? directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                await _directoryUtil.Create(directory, log: false, cancellationToken: cancellationToken).NoSync();

            await _fileUtil!.Write(fullPath, file.Content, log: false, cancellationToken: cancellationToken).NoSync();
        }
    }
}
