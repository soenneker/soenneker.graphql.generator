using System.Text;
using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Models;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates operation clients, request builders, and selection-set building. Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private GeneratedFile GenerateGraphQlClientRoot(GraphQLObjectTypeDefinition? queryRoot, GraphQLObjectTypeDefinition? mutationRoot)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        AppendDescription(sb, "Root GraphQL client; exposes one request builder per query and mutation operation. Create with: new GraphQlClient(new GraphQlHttpClient(httpClient)) or pass any IGraphQlClient implementation.", 0);
        sb.AppendLine("public sealed partial class GraphQlClient");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IGraphQlClient _graphQlClient;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Creates a GraphQL client. Pass an IGraphQlClient implementation, e.g. <c>new GraphQlHttpClient(httpClient)</c> for HTTP.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public GraphQlClient(IGraphQlClient graphQlClient)");
        sb.AppendLine("    {");
        sb.AppendLine("        _graphQlClient = graphQlClient;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (queryRoot?.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in queryRoot.Fields.Items)
            {
                string fieldName = NameOf(field.Name);
                string requestBuilderName = CSharpNaming.ToOperationRequestBuilderName(fieldName, "Query");
                string propertyName = CSharpNaming.ToOperationBuilderPropertyName(fieldName, "Query");
                sb.AppendLine("    /// <summary>");
                sb.Append("    /// Builds and executes requests for the '").Append(fieldName).Append("' query.</summary>").AppendLine();
                sb.Append("    public ").Append(requestBuilderName).Append(' ').Append(propertyName).Append(" => new ").Append(requestBuilderName).AppendLine("(_graphQlClient);");
                sb.AppendLine();
            }
        }

        if (mutationRoot?.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in mutationRoot.Fields.Items)
            {
                string fieldName = NameOf(field.Name);
                string requestBuilderName = CSharpNaming.ToOperationRequestBuilderName(fieldName, "Mutation");
                string propertyName = CSharpNaming.ToOperationBuilderPropertyName(fieldName, "Mutation");
                sb.AppendLine("    /// <summary>");
                sb.Append("    /// Builds and executes requests for the '").Append(fieldName).Append("' mutation.</summary>").AppendLine();
                sb.Append("    public ").Append(requestBuilderName).Append(' ').Append(propertyName).Append(" => new ").Append(requestBuilderName).AppendLine("(_graphQlClient);");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return new GeneratedFile("Clients/GraphQlClient.cs", sb.ToString());
    }

    private IReadOnlyList<GeneratedFile> GenerateOperationArtifacts(GraphQLObjectTypeDefinition rootType, string classPrefix)
    {
        var files = new List<GeneratedFile>();

        if (rootType.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in rootType.Fields.Items)
            {
                string fieldName = NameOf(field.Name);
                (string resourceFolder, string? operationFolder) = CSharpNaming.GetOperationPathSegments(fieldName);
                string pathSegment = operationFolder is not null ? $"{resourceFolder}/{operationFolder}" : resourceFolder;

                string wrapperTypeName = CSharpNaming.ToOperationDataTypeName(fieldName, classPrefix);
                files.Add(GenerateOperationResponseWrapper(field, wrapperTypeName, pathSegment));

                string requestBuilderName = CSharpNaming.ToOperationRequestBuilderName(fieldName, classPrefix);

                var args = field.Arguments?.Items?.ToList() ?? [];
                if (args.Count > 0)
                {
                    string requestTypeName = CSharpNaming.ToOperationRequestName(fieldName, classPrefix);
                    files.Add(GenerateOperationRequestType(field, requestTypeName, pathSegment));
                    files.Add(GenerateOperationRequestBuilder(field, classPrefix, wrapperTypeName, requestTypeName, requestBuilderName, pathSegment));
                }
                else
                {
                    files.Add(GenerateOperationRequestBuilder(field, classPrefix, wrapperTypeName, null, requestBuilderName, pathSegment));
                }
            }
        }

        return files;
    }

    private GeneratedFile GenerateOperationRequestType(GraphQLFieldDefinition field, string requestTypeName, string pathSegment)
    {
        var args = field.Arguments?.Items?.ToList() ?? [];
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, $"Request parameters for the '{NameOf(field.Name)}' GraphQL operation.", 0);
        sb.Append("public sealed class ").Append(requestTypeName).AppendLine();
        sb.AppendLine("{");
        foreach (GraphQLInputValueDefinition arg in args)
        {
            string propertyType = MapInputType(arg.Type);
            string propertyName = CSharpNaming.ToClrPropertyName(NameOf(arg.Name), requestTypeName);
            string? argDescription = GetDescription(arg.Description);
            AppendDescription(sb, argDescription, 1);
            sb.Append("    public ").Append(propertyType).Append(' ').Append(propertyName).Append(" { get; init; }");
            if (ShouldInitializeCollection(arg.Type, propertyType))
                sb.Append(" = [];");
            else if (IsReferenceTypeNeedingNullForgiving(arg.Type, propertyType))
                sb.Append(" = null!;");
            sb.AppendLine();
            sb.AppendLine();
        }
        sb.AppendLine("}");
        return new GeneratedFile($"Clients/{pathSegment}/{requestTypeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateOperationRequestBuilder(GraphQLFieldDefinition field, string operationKind, string wrapperTypeName, string? requestTypeName, string requestBuilderName, string pathSegment)
    {
        string fieldName = NameOf(field.Name);
        string executeMethodName = CSharpNaming.ToOperationMethodName(fieldName, operationKind);
        string resultClrType = MapOutputType(field.Type);
        string nullableResultType = resultClrType.EndsWith("?", StringComparison.Ordinal) ? resultClrType : resultClrType + "?";
        string wrapperPropertyName = CSharpNaming.ToClrPropertyName(fieldName, wrapperTypeName);
        var args = field.Arguments?.Items?.ToList() ?? [];
        string selectionSet = BuildSelectionSet(field.Type, _config.MaxSelectionDepth);
        string variableDefinitions = BuildOperationVariableDefinitions(args);
        string fieldArguments = BuildFieldArgumentList(args);
        string gqlOperationType = operationKind.Equals("Mutation", StringComparison.Ordinal) ? "mutation" : "query";

        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        AppendDescription(sb, $"Builds and executes requests for the '{fieldName}' GraphQL {gqlOperationType}.", 0);
        sb.Append("public sealed partial class ").Append(requestBuilderName).AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IGraphQlClient _graphQlClient;");
        sb.AppendLine();
        sb.Append("    public ").Append(requestBuilderName).AppendLine("(IGraphQlClient graphQlClient)");
        sb.AppendLine("    {");
        sb.AppendLine("        _graphQlClient = graphQlClient;");
        sb.AppendLine("    }");
        sb.AppendLine();

        if (requestTypeName is not null)
        {
            AppendDescription(sb, $"Executes the GraphQL {gqlOperationType} '{fieldName}' with the given request parameters.", 1);
            sb.Append("    public Task<GraphQlResponse<").Append(wrapperTypeName).Append(">> ExecuteAsync(").Append(requestTypeName).Append(" request, CancellationToken cancellationToken = default)").AppendLine();
            sb.AppendLine("    {");
            sb.Append("        const string gqlQuery = @\"").Append(gqlOperationType).Append(' ').Append(executeMethodName);
            if (variableDefinitions.Length > 0)
                sb.Append('(').Append(variableDefinitions).Append(')');
            sb.Append(" { ").Append(fieldName);
            if (fieldArguments.Length > 0)
                sb.Append('(').Append(fieldArguments).Append(')');
            sb.Append(' ');
            if (!string.IsNullOrWhiteSpace(selectionSet))
                sb.Append(selectionSet).Append(' ');
            sb.Append("}\";").AppendLine();
            sb.AppendLine("        object variables = new");
            sb.AppendLine("        {");
            for (int i = 0; i < args.Count; i++)
            {
                GraphQLInputValueDefinition arg = args[i];
                string clrArgName = CSharpNaming.ToClrPropertyName(NameOf(arg.Name), requestTypeName);
                string camel = CSharpNaming.ToCamelCase(clrArgName);
                string camelSafe = CSharpNaming.SafeParameterName(camel);
                sb.Append("            ").Append(camelSafe).Append(" = request.").Append(clrArgName);
                if (i < args.Count - 1) sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("        };");
            sb.Append("        return _graphQlClient.ExecuteAsync<").Append(wrapperTypeName).Append(">(gqlQuery, variables, cancellationToken);").AppendLine();
            sb.AppendLine("    }");
            sb.AppendLine();
            AppendDescription(sb, $"Executes the operation and returns only the '{fieldName}' value from the GraphQL data envelope.", 1);
            sb.Append("    public async Task<").Append(nullableResultType).Append("> GetValueAsync(").Append(requestTypeName).Append(" request, CancellationToken cancellationToken = default)").AppendLine();
            sb.AppendLine("    {");
            sb.Append("        GraphQlResponse<").Append(wrapperTypeName).Append("> response = await ExecuteAsync(request, cancellationToken).ConfigureAwait(false);").AppendLine();
            sb.Append("        return response.Data?.").Append(wrapperPropertyName).AppendLine(";");
            sb.AppendLine("    }");
        }
        else
        {
            AppendDescription(sb, $"Executes the GraphQL {gqlOperationType} '{fieldName}'.", 1);
            sb.Append("    public Task<GraphQlResponse<").Append(wrapperTypeName).Append(">> ExecuteAsync(CancellationToken cancellationToken = default)").AppendLine();
            sb.AppendLine("    {");
            sb.Append("        const string gqlQuery = @\"").Append(gqlOperationType).Append(' ').Append(executeMethodName).Append(" { ").Append(fieldName);
            if (!string.IsNullOrWhiteSpace(selectionSet))
                sb.Append(' ').Append(selectionSet).Append(' ');
            sb.Append("}\";").AppendLine();
            sb.AppendLine("        return _graphQlClient.ExecuteAsync<" + wrapperTypeName + ">(gqlQuery, null, cancellationToken);");
            sb.AppendLine("    }");
            sb.AppendLine();
            AppendDescription(sb, $"Executes the operation and returns only the '{fieldName}' value from the GraphQL data envelope.", 1);
            sb.Append("    public async Task<").Append(nullableResultType).Append("> GetValueAsync(CancellationToken cancellationToken = default)").AppendLine();
            sb.AppendLine("    {");
            sb.Append("        GraphQlResponse<").Append(wrapperTypeName).Append("> response = await ExecuteAsync(cancellationToken).ConfigureAwait(false);").AppendLine();
            sb.Append("        return response.Data?.").Append(wrapperPropertyName).AppendLine(";");
            sb.AppendLine("    }");
        }

        sb.AppendLine("}");
        return new GeneratedFile($"Clients/{pathSegment}/{requestBuilderName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateOperationResponseWrapper(GraphQLFieldDefinition field, string wrapperTypeName, string pathSegment)
    {
        string fieldName = NameOf(field.Name);
        string fieldClrType = MapOutputType(field.Type);
        string propertyName = CSharpNaming.ToClrPropertyName(fieldName, wrapperTypeName);
        string? description = GetDescription(field.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, $"Response data wrapper for the '{fieldName}' GraphQL operation.", 0);
        sb.Append("public sealed partial class ").Append(wrapperTypeName).AppendLine();
        sb.AppendLine("{");
        AppendDescription(sb, description, 1);
        sb.Append("    public ").Append(fieldClrType).Append(' ').Append(propertyName).Append(" { get; init; }");
        if (ShouldInitializeCollection(field.Type, fieldClrType))
            sb.Append(" = [];");
        else if (IsReferenceTypeNeedingNullForgiving(field.Type, fieldClrType))
            sb.Append(" = null!;");
        sb.AppendLine();
        sb.AppendLine("}");
        return new GeneratedFile($"Operations/{pathSegment}/{wrapperTypeName}.cs", sb.ToString());
    }

    private string BuildSelectionSet(GraphQLType fieldType, int maxDepth)
    {
        string namedType = GetNamedType(fieldType);
        if (IsScalarOrEnum(namedType)) return string.Empty;
        if (_objectTypes.TryGetValue(namedType, out GraphQLObjectTypeDefinition? obj))
            return BuildSelectionSetForObject(obj, maxDepth, new HashSet<string>(StringComparer.Ordinal));
        if (_interfaceTypes.TryGetValue(namedType, out GraphQLInterfaceTypeDefinition? iface))
            return BuildSelectionSetForInterface(iface, maxDepth, new HashSet<string>(StringComparer.Ordinal));
        return string.Empty;
    }

    private string BuildSelectionSetForObject(GraphQLObjectTypeDefinition obj, int remainingDepth, HashSet<string> visited)
    {
        string objectName = NameOf(obj.Name);
        if (remainingDepth < 0 || !visited.Add(objectName)) return string.Empty;
        try
        {
            var fields = new List<string>();
            if (obj.Fields?.Items is { Count: > 0 })
            {
                foreach (GraphQLFieldDefinition field in obj.Fields.Items)
                {
                    string fieldName = NameOf(field.Name);
                    string namedType = GetNamedType(field.Type);
                    if (field.Arguments?.Items is { Count: > 0 })
                    {
                        if (IsUsefulSpecialField(fieldName))
                        {
                            string nestedSpecial = BuildSpecialNestedSelection(fieldName, field.Type, remainingDepth, visited);
                            if (!string.IsNullOrWhiteSpace(nestedSpecial))
                                fields.Add($"{fieldName} {nestedSpecial}");
                        }
                        continue;
                    }
                    if (IsScalarOrEnum(namedType)) { fields.Add(fieldName); continue; }
                    if (remainingDepth == 0) continue;
                    if (TryBuildConnectionSelection(fieldName, field.Type, remainingDepth, visited, out string? connectionSelection))
                    {
                        fields.Add($"{fieldName} {connectionSelection}");
                        continue;
                    }
                    if (_objectTypes.TryGetValue(namedType, out GraphQLObjectTypeDefinition? nestedObject))
                    {
                        string nested = BuildSelectionSetForObject(nestedObject, remainingDepth - 1, visited);
                        if (!string.IsNullOrWhiteSpace(nested)) fields.Add($"{fieldName} {nested}");
                    }
                    else if (_interfaceTypes.TryGetValue(namedType, out GraphQLInterfaceTypeDefinition? nestedInterface))
                    {
                        string nested = BuildSelectionSetForInterface(nestedInterface, remainingDepth - 1, visited);
                        if (!string.IsNullOrWhiteSpace(nested)) fields.Add($"{fieldName} {nested}");
                    }
                    else if (IsUsefulSpecialField(fieldName))
                    {
                        string nested = BuildSpecialNestedSelection(fieldName, field.Type, remainingDepth - 1, visited);
                        if (!string.IsNullOrWhiteSpace(nested)) fields.Add($"{fieldName} {nested}");
                    }
                }
            }
            if (fields.Count == 0) return "{ id }";
            return "{ " + string.Join(" ", fields.Distinct(StringComparer.Ordinal)) + " }";
        }
        finally { visited.Remove(objectName); }
    }

    private string BuildSelectionSetForInterface(GraphQLInterfaceTypeDefinition iface, int remainingDepth, HashSet<string> visited)
    {
        string interfaceName = NameOf(iface.Name);
        if (remainingDepth < 0 || !visited.Add(interfaceName)) return string.Empty;
        try
        {
            var fields = new List<string>();
            if (iface.Fields?.Items is { Count: > 0 })
            {
                foreach (GraphQLFieldDefinition field in iface.Fields.Items)
                {
                    if (field.Arguments?.Items is { Count: > 0 }) continue;
                    string fieldName = NameOf(field.Name);
                    string namedType = GetNamedType(field.Type);
                    if (IsScalarOrEnum(namedType)) fields.Add(fieldName);
                }
            }
            if (fields.Count == 0) return string.Empty;
            return "{ " + string.Join(" ", fields.Distinct(StringComparer.Ordinal)) + " }";
        }
        finally { visited.Remove(interfaceName); }
    }

    private bool TryBuildConnectionSelection(string fieldName, GraphQLType fieldType, int remainingDepth, HashSet<string> visited, out string? selection)
    {
        selection = null;
        string namedType = GetNamedType(fieldType);
        if (!_objectTypes.TryGetValue(namedType, out GraphQLObjectTypeDefinition? obj)) return false;
        bool hasNodes = HasField(obj, "nodes");
        bool hasEdges = HasField(obj, "edges");
        bool hasPageInfo = HasField(obj, "pageInfo");
        if (!hasNodes && !hasEdges && !hasPageInfo) return false;
        var parts = new List<string>();
        if (hasNodes)
        {
            GraphQLFieldDefinition? nodesField = GetField(obj, "nodes");
            if (nodesField is not null)
            {
                string nodeNamedType = GetNamedType(nodesField.Type);
                if (_objectTypes.TryGetValue(nodeNamedType, out GraphQLObjectTypeDefinition? nodeType))
                {
                    string nested = BuildSelectionSetForObject(nodeType, remainingDepth - 1, visited);
                    parts.Add(string.IsNullOrWhiteSpace(nested) ? "nodes { id }" : $"nodes {nested}");
                }
                else parts.Add("nodes");
            }
        }
        else if (hasEdges)
        {
            GraphQLFieldDefinition? edgesField = GetField(obj, "edges");
            if (edgesField is not null)
            {
                string edgeTypeName = GetNamedType(edgesField.Type);
                if (_objectTypes.TryGetValue(edgeTypeName, out GraphQLObjectTypeDefinition? edgeType))
                {
                    GraphQLFieldDefinition? nodeField = GetField(edgeType, "node");
                    if (nodeField is not null)
                    {
                        string nodeNamedType = GetNamedType(nodeField.Type);
                        if (_objectTypes.TryGetValue(nodeNamedType, out GraphQLObjectTypeDefinition? nodeType))
                        {
                            string nested = BuildSelectionSetForObject(nodeType, remainingDepth - 1, visited);
                            parts.Add(string.IsNullOrWhiteSpace(nested) ? "edges { cursor node { id } }" : $"edges {{ cursor node {nested} }}");
                        }
                    }
                }
            }
        }
        if (hasPageInfo) parts.Add("pageInfo { hasNextPage hasPreviousPage startCursor endCursor }");
        if (parts.Count == 0) return false;
        selection = "{ " + string.Join(" ", parts) + " }";
        return true;
    }

    private string BuildSpecialNestedSelection(string fieldName, GraphQLType fieldType, int remainingDepth, HashSet<string> visited)
    {
        if (string.Equals(fieldName, "userErrors", StringComparison.OrdinalIgnoreCase) || string.Equals(fieldName, "errors", StringComparison.OrdinalIgnoreCase))
            return "{ field message code }";
        string namedType = GetNamedType(fieldType);
        if (_objectTypes.TryGetValue(namedType, out GraphQLObjectTypeDefinition? obj))
            return BuildSelectionSetForObject(obj, remainingDepth, visited);
        if (_interfaceTypes.TryGetValue(namedType, out GraphQLInterfaceTypeDefinition? iface))
            return BuildSelectionSetForInterface(iface, remainingDepth, visited);
        return string.Empty;
    }

    private static bool IsUsefulSpecialField(string fieldName) =>
        fieldName is "userErrors" or "errors" or "pageInfo" or "nodes" or "edges" or "node";

    private bool HasField(GraphQLObjectTypeDefinition obj, string fieldName) =>
        obj.Fields?.Items?.Any(f => string.Equals(NameOf(f.Name), fieldName, StringComparison.Ordinal)) == true;

    private GraphQLFieldDefinition? GetField(GraphQLObjectTypeDefinition obj, string fieldName) =>
        obj.Fields?.Items?.FirstOrDefault(f => string.Equals(NameOf(f.Name), fieldName, StringComparison.Ordinal));

    private static string BuildMethodParameterList(List<GraphQLInputValueDefinition> args)
    {
        if (args.Count == 0) return string.Empty;
        var parts = new List<string>(args.Count);
        foreach (GraphQLInputValueDefinition arg in args)
        {
            string parameterType = MapGraphQlInputTypeStatic(arg.Type);
            string parameterName = CSharpNaming.ToCamelCase(CSharpNaming.ToClrPropertyName(NameOf(arg.Name), "Arg"));
            parts.Add($"{parameterType} {CSharpNaming.SafeParameterName(parameterName)}");
        }
        return string.Join(", ", parts);
    }

    private static string BuildOperationVariableDefinitions(List<GraphQLInputValueDefinition> args)
    {
        if (args.Count == 0) return string.Empty;
        return string.Join(", ", args.Select(a => $"${NameOf(a.Name)}: {ToGraphQlTypeString(a.Type)}"));
    }

    private static string BuildFieldArgumentList(List<GraphQLInputValueDefinition> args)
    {
        if (args.Count == 0) return string.Empty;
        return string.Join(", ", args.Select(a => $"{NameOf(a.Name)}: ${NameOf(a.Name)}"));
    }
}
