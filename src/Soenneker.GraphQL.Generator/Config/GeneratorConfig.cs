namespace Soenneker.GraphQL.Generator.Config;

/// <summary>
/// Configuration for GraphQL schema to C# code generation.
/// </summary>
public sealed class GeneratorConfig
{
    /// <summary>Path to the GraphQL SDL schema file (used by CLI/host).</summary>
    public required string SchemaPath { get; init; }

    /// <summary>Directory where generated files will be written (used by CLI/host).</summary>
    public required string OutputDirectory { get; init; }

    /// <summary>Root namespace for generated types.</summary>
    public required string Namespace { get; init; }

    /// <summary>CLR type used for GraphQL ID scalar. Default: <c>string</c>.</summary>
    public string IdClrType { get; init; } = "string";

    /// <summary>Whether to emit Query, Mutation, and Subscription root types. Default: <c>true</c>.</summary>
    public bool EmitRootTypes { get; init; } = true;

    /// <summary>Whether to emit global type aliases for custom scalars. Default: <c>false</c>.</summary>
    public bool EmitScalarAliases { get; init; } = false;

    /// <summary>Whether to emit a JsonSerializerContext for source-generated JSON. Default: <c>true</c>.</summary>
    public bool EmitJsonSerializerContext { get; init; } = true;

    /// <summary>Name of the generated JsonSerializerContext class. Default: <c>GraphQlJsonContext</c>.</summary>
    public string JsonSerializerContextName { get; init; } = "GraphQlJsonContext";

    /// <summary>Whether to emit Query and Mutation operation clients. Default: <c>true</c>.</summary>
    public bool EmitOperationClients { get; init; } = true;

    /// <summary>GraphQL root type name for queries. Default: <c>Query</c>.</summary>
    public string QueryRootTypeName { get; init; } = "Query";

    /// <summary>GraphQL root type name for mutations. Default: <c>Mutation</c>.</summary>
    public string MutationRootTypeName { get; init; } = "Mutation";

    /// <summary>Max depth for auto-generated selection sets. Default: <c>2</c>.</summary>
    public int MaxSelectionDepth { get; init; } = 2;

    /// <summary>Optional global usings to add to each generated file.</summary>
    public List<string>? GlobalUsings { get; init; }

    /// <summary>Optional overrides for GraphQL scalar → CLR type mapping.</summary>
    public Dictionary<string, string>? ScalarMappings { get; init; }
}