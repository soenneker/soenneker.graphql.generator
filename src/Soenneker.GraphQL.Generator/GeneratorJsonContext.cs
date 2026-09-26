using System.Text.Json;
using System.Text.Json.Serialization;
using Soenneker.GraphQL.Generator.Config;

namespace Soenneker.GraphQL.Generator;

[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(GeneratorConfig))]
internal partial class GeneratorJsonContext : JsonSerializerContext;
