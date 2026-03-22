using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates C# types and optional JsonSerializerContext from a parsed GraphQL document.
/// Generation logic is split across partial class files by concern: type mapping, type generation,
/// transport types, operation clients, JSON context, and formatting.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private readonly GeneratorConfig _config;
    private readonly IReadOnlyDictionary<string, string> _scalarMap;
    private readonly HashSet<string> _definedScalars;
    private readonly HashSet<string> _definedEnums;
    private readonly Dictionary<string, GraphQLObjectTypeDefinition> _objectTypes;
    private readonly Dictionary<string, GraphQLInputObjectTypeDefinition> _inputTypes;
    private readonly Dictionary<string, GraphQLInterfaceTypeDefinition> _interfaceTypes;
    private readonly Dictionary<string, GraphQLUnionTypeDefinition> _unionTypes;

    public SchemaGenerator(GeneratorConfig config, GraphQLDocument document)
    {
        _config = config;
        _scalarMap = ScalarMapping.CreateScalarMap(config);
        _definedScalars = new HashSet<string>(StringComparer.Ordinal);
        _definedEnums = new HashSet<string>(StringComparer.Ordinal);
        _objectTypes = new Dictionary<string, GraphQLObjectTypeDefinition>(StringComparer.Ordinal);
        _inputTypes = new Dictionary<string, GraphQLInputObjectTypeDefinition>(StringComparer.Ordinal);
        _interfaceTypes = new Dictionary<string, GraphQLInterfaceTypeDefinition>(StringComparer.Ordinal);
        _unionTypes = new Dictionary<string, GraphQLUnionTypeDefinition>(StringComparer.Ordinal);

        foreach (ASTNode definition in document.Definitions)
        {
            switch (definition)
            {
                case GraphQLScalarTypeDefinition scalar:
                    _definedScalars.Add(NameOf(scalar.Name));
                    break;
                case GraphQLEnumTypeDefinition enm:
                    _definedEnums.Add(CSharpNaming.ToClrTypeName(NameOf(enm.Name)));
                    break;
                case GraphQLObjectTypeDefinition obj:
                    _objectTypes[NameOf(obj.Name)] = obj;
                    break;
                case GraphQLInputObjectTypeDefinition input:
                    _inputTypes[NameOf(input.Name)] = input;
                    break;
                case GraphQLInterfaceTypeDefinition iface:
                    _interfaceTypes[NameOf(iface.Name)] = iface;
                    break;
                case GraphQLUnionTypeDefinition union:
                    _unionTypes[NameOf(union.Name)] = union;
                    break;
            }
        }
    }

    public GenerationResult Generate(GraphQLDocument document)
    {
        var files = new List<GeneratedFile>();
        int objectCount = 0, inputCount = 0, enumCount = 0, interfaceCount = 0, unionCount = 0, scalarCount = 0, operationCount = 0;

        foreach (ASTNode definition in document.Definitions)
        {
            switch (definition)
            {
                case GraphQLObjectTypeDefinition obj:
                    if (IsBuiltInRootType(NameOf(obj.Name)) && !_config.EmitRootTypes)
                        continue;
                    files.Add(GenerateObjectType(obj));
                    objectCount++;
                    break;
                case GraphQLInputObjectTypeDefinition input:
                    files.Add(GenerateInputType(input));
                    inputCount++;
                    break;
                case GraphQLEnumTypeDefinition enm:
                    files.Add(GenerateEnumType(enm));
                    enumCount++;
                    break;
                case GraphQLInterfaceTypeDefinition iface:
                    files.Add(GenerateInterfaceType(iface));
                    interfaceCount++;
                    break;
                case GraphQLUnionTypeDefinition union:
                    files.Add(GenerateUnionType(union));
                    unionCount++;
                    break;
                case GraphQLScalarTypeDefinition scalar:
                    if (_config.EmitScalarAliases)
                        files.Add(GenerateScalarAlias(scalar));
                    scalarCount++;
                    break;
            }
        }

        files.AddRange(GenerateTransportFiles());

        if (_config.EmitOperationClients)
        {
            IReadOnlyList<GeneratedFile> operationFiles = GenerateOperationFiles();
            files.AddRange(operationFiles);
            operationCount += operationFiles.Count;
        }

        if (_config.EmitJsonSerializerContext)
            files.Add(GenerateJsonContext(document));

        return new GenerationResult(files, objectCount, inputCount, enumCount, interfaceCount, unionCount, scalarCount, operationCount);
    }

    private static string NameOf(GraphQLName name) => name.Value.ToString();

    private IReadOnlyList<GeneratedFile> GenerateTransportFiles() =>
    [
        GenerateGraphQlRequestFile(),
        GenerateGraphQlErrorFile(),
        GenerateGraphQlResponseFile(),
        GenerateIGraphQlClientFile(),
        GenerateGraphQlHttpClientFile()
    ];

    private IReadOnlyList<GeneratedFile> GenerateOperationFiles()
    {
        var files = new List<GeneratedFile>();
        GraphQLObjectTypeDefinition? queryRoot = FindObjectType(_config.QueryRootTypeName);
        GraphQLObjectTypeDefinition? mutationRoot = FindObjectType(_config.MutationRootTypeName);
        IReadOnlyList<OperationLayout> queryLayouts = GetOperationLayouts(queryRoot, "Query");
        IReadOnlyList<OperationLayout> mutationLayouts = GetOperationLayouts(mutationRoot, "Mutation");

        if (queryRoot is not null)
            files.AddRange(GenerateOperationArtifacts(queryRoot, queryLayouts, "Query"));

        if (mutationRoot is not null)
            files.AddRange(GenerateOperationArtifacts(mutationRoot, mutationLayouts, "Mutation"));

        files.AddRange(GenerateGroupedOperationBuilderFiles(queryLayouts, mutationLayouts));

        if (queryRoot is not null || mutationRoot is not null)
            files.Add(GenerateGraphQlClientRoot(queryLayouts, mutationLayouts));

        return files;
    }

    private GraphQLObjectTypeDefinition? FindObjectType(string typeName)
    {
        _objectTypes.TryGetValue(typeName, out GraphQLObjectTypeDefinition? result);
        return result;
    }

    private static bool IsBuiltInRootType(string name)
        => name is "Query" or "Mutation" or "Subscription" or "QueryRoot";
}
