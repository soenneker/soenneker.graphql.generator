using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Soenneker.GraphQL.Generator;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Models;

const string usage = """
gql2cs - GraphQL SDL to C# source generator

Usage:
  gql2cs --config <path-to-config.json>

Example:
  gql2cs --config ./config.json
""";

if (args.Length == 0)
{
    Console.WriteLine(usage);
    return 1;
}

string? configPath = null;
for (var i = 0; i < args.Length; i++)
{
    if (string.Equals(args[i], "--config", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
    {
        configPath = args[i + 1];
        i++;
    }
}

if (string.IsNullOrWhiteSpace(configPath))
{
    Console.Error.WriteLine("Missing required --config argument.");
    Console.WriteLine(usage);
    return 2;
}

if (!File.Exists(configPath))
{
    Console.Error.WriteLine($"Config file not found: {configPath}");
    return 3;
}

GeneratorConfig config;
try
{
    string configJson = await File.ReadAllTextAsync(configPath);
    config = JsonSerializer.Deserialize<GeneratorConfig>(configJson, JsonOptions.Default)
             ?? throw new InvalidOperationException("Failed to deserialize config.");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read config: {ex.Message}");
    return 4;
}

if (string.IsNullOrWhiteSpace(config.SchemaPath))
{
    Console.Error.WriteLine("SchemaPath is required in config.");
    return 5;
}

if (!File.Exists(config.SchemaPath))
{
    Console.Error.WriteLine($"Schema file not found: {config.SchemaPath}");
    return 6;
}

if (string.IsNullOrWhiteSpace(config.OutputDirectory))
{
    Console.Error.WriteLine("OutputDirectory is required in config.");
    return 7;
}

Directory.CreateDirectory(config.OutputDirectory);

string schemaText;
try
{
    schemaText = await File.ReadAllTextAsync(config.SchemaPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read schema: {ex.Message}");
    return 8;
}

IGraphQLGenerator generator = new GraphQLGenerator();
GenerationResult result;
try
{
    result = generator.Generate(schemaText, config);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to parse GraphQL schema: {ex.Message}");
    return 9;
}

foreach (var file in result.Files)
{
    string fullPath = Path.Combine(config.OutputDirectory, file.RelativePath);
    string? dir = Path.GetDirectoryName(fullPath);
    if (!string.IsNullOrWhiteSpace(dir))
        Directory.CreateDirectory(dir);
    await File.WriteAllTextAsync(fullPath, file.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

Console.WriteLine($"Generated {result.Files.Count} file(s) into '{Path.GetFullPath(config.OutputDirectory)}'.");
Console.WriteLine($"Objects: {result.ObjectCount}");
Console.WriteLine($"Inputs: {result.InputCount}");
Console.WriteLine($"Enums: {result.EnumCount}");
Console.WriteLine($"Interfaces: {result.InterfaceCount}");
Console.WriteLine($"Unions: {result.UnionCount}");
Console.WriteLine($"Scalars: {result.ScalarCount}");
Console.WriteLine($"Operation files: {result.OperationFileCount}");
return 0;

static class JsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
