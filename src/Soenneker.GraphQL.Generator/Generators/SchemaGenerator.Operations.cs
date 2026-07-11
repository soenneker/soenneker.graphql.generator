using Soenneker.Utils.PooledStringBuilders;
using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates operation clients, request builders, and selection-set building. Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private GeneratedFile GenerateGraphQlClientRoot(IReadOnlyList<OperationLayout> queryLayouts, IReadOnlyList<OperationLayout> mutationLayouts)
    {
        string entryClientName = GetEntryClientTypeName();
        IReadOnlyList<OperationGroup> operationGroups = GetOperationGroups(queryLayouts, mutationLayouts);
        var sb = new PooledStringBuilder();
        try
        {
        AppendHeader(ref sb);
        AppendDescription(ref sb, $"Root GraphQL client; exposes direct and grouped request builders for GraphQL queries and mutations. Create with: new {entryClientName}(new GraphQlHttpClient(httpClient)) or pass any IGraphQlClient implementation.", 0);
        sb.Append("public sealed partial class ");
        sb.Append(entryClientName);
        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IGraphQlClient _graphQlClient;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Creates a GraphQL client. Pass an IGraphQlClient implementation, e.g. <c>new GraphQlHttpClient(httpClient)</c> for HTTP.");
        sb.AppendLine("    /// </summary>");
        sb.Append("    public ");
        sb.Append(entryClientName);
        sb.AppendLine("(IGraphQlClient graphQlClient)");
        sb.AppendLine("    {");
        sb.AppendLine("        _graphQlClient = graphQlClient;");
        sb.AppendLine("    }");
        sb.AppendLine();

        foreach (OperationGroup group in operationGroups)
        {
            string builderName = CSharpNaming.ToOperationGroupBuilderName(group.FullName);
            string propertyName = CSharpNaming.ToClrPropertyName(group.SegmentName, entryClientName);
            sb.AppendLine("    /// <summary>");
            sb.Append("    /// Builds and executes grouped requests for the '");
            sb.Append(CSharpNaming.ToCamelCase(group.FullName));
            sb.Append("' resource.</summary>");
            sb.AppendLine();
            sb.Append("    public ");
            sb.Append(builderName);
            sb.Append(' ');
            sb.Append(propertyName);
            sb.Append(" => new ");
            sb.Append(builderName);
            sb.AppendLine("(_graphQlClient);");
            sb.AppendLine();
        }

        foreach (OperationLayout layout in queryLayouts)
        {
            if (layout.GroupSegments.Count > 0)
                continue;

            string propertyName = CSharpNaming.ToOperationBuilderPropertyName(layout.FieldName, layout.OperationKind);
            sb.AppendLine("    /// <summary>");
            sb.Append("    /// Builds and executes requests for the '");
            sb.Append(layout.FieldName);
            sb.Append("' ");
            sb.Append(layout.OperationKind.ToLowerInvariant());
            sb.Append(".</summary>");
            sb.AppendLine();
            sb.Append("    public ");
            sb.Append(layout.RequestBuilderName);
            sb.Append(' ');
            sb.Append(propertyName);
            sb.Append(" => new ");
            sb.Append(layout.RequestBuilderName);
            sb.AppendLine("(_graphQlClient);");
            sb.AppendLine();
        }

        foreach (OperationLayout layout in mutationLayouts)
        {
            if (layout.GroupSegments.Count > 0)
                continue;

            string propertyName = CSharpNaming.ToOperationBuilderPropertyName(layout.FieldName, layout.OperationKind);
            sb.AppendLine("    /// <summary>");
            sb.Append("    /// Builds and executes requests for the '");
            sb.Append(layout.FieldName);
            sb.Append("' ");
            sb.Append(layout.OperationKind.ToLowerInvariant());
            sb.Append(".</summary>");
            sb.AppendLine();
            sb.Append("    public ");
            sb.Append(layout.RequestBuilderName);
            sb.Append(' ');
            sb.Append(propertyName);
            sb.Append(" => new ");
            sb.Append(layout.RequestBuilderName);
            sb.AppendLine("(_graphQlClient);");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        return new GeneratedFile($"{entryClientName}.cs", sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }
    }

    private IReadOnlyList<GeneratedFile> GenerateOperationArtifacts(GraphQLObjectTypeDefinition rootType, IReadOnlyList<OperationLayout> operationLayouts, string classPrefix)
    {
        var files = new List<GeneratedFile>();
        Dictionary<string, OperationLayout> layoutMap = operationLayouts.ToDictionary(static layout => layout.FieldName, StringComparer.Ordinal);

        if (rootType.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in rootType.Fields.Items)
            {
                string fieldName = NameOf(field.Name);
                string pathSegment;

                if (layoutMap.TryGetValue(fieldName, out OperationLayout? layout))
                {
                    pathSegment = layout.PathSegment;
                }
                else
                {
                    pathSegment = CSharpNaming.ToClrTypeName(fieldName);
                }

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
        var usings = CreateUsingSet(["System.Text.Json.Serialization"]);

        foreach (GraphQLInputValueDefinition arg in args)
        {
            string propertyType = MapInputType(arg.Type);
            AddUsingsForType(usings, propertyType);
        }

        var sb = new PooledStringBuilder();
        try
        {
        AppendHeader(ref sb, usings);
        AppendDescription(ref sb, $"Request parameters for the '{NameOf(field.Name)}' GraphQL operation.", 0);
        sb.Append("public sealed class ");
        sb.Append(requestTypeName);
        sb.AppendLine();
        sb.AppendLine("{");
        foreach (GraphQLInputValueDefinition arg in args)
        {
            string propertyType = MapInputType(arg.Type);
            string propertyName = CSharpNaming.ToClrPropertyName(NameOf(arg.Name), requestTypeName);
            string? argDescription = GetDescription(arg.Description);
            AppendDescription(ref sb, argDescription, 1);
            sb.Append("    [JsonPropertyName(\"");
            sb.Append(NameOf(arg.Name));
            sb.AppendLine("\")]");
            sb.Append("    public ");
            sb.Append(propertyType);
            sb.Append(' ');
            sb.Append(propertyName);
            sb.Append(" { get; init; }");
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
        finally
        {
            sb.Dispose();
        }
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
        var usings = CreateUsingSet(["System.Threading", "System.Threading.Tasks"]);
        AddUsingsForType(usings, nullableResultType);

        var sb = new PooledStringBuilder();
        try
        {
            AppendHeader(ref sb, usings);
            AppendDescription(ref sb, $"Builds and executes requests for the '{fieldName}' GraphQL {gqlOperationType}.", 0);
            sb.Append("public sealed partial class ");
            sb.Append(requestBuilderName);
            sb.AppendLine();
            sb.AppendLine("{");
            sb.AppendLine("    private readonly IGraphQlClient _graphQlClient;");
            sb.AppendLine();
            sb.Append("    public ");
            sb.Append(requestBuilderName);
            sb.AppendLine("(IGraphQlClient graphQlClient)");
            sb.AppendLine("    {");
            sb.AppendLine("        _graphQlClient = graphQlClient;");
            sb.AppendLine("    }");
            sb.AppendLine();

            if (requestTypeName is not null)
            {
                AppendDescription(ref sb, $"Executes the GraphQL {gqlOperationType} '{fieldName}' with the given request parameters.", 1);
                sb.Append("    public ValueTask<GraphQlResponse<");
                sb.Append(wrapperTypeName);
                sb.Append(">> Execute(");
                sb.Append(requestTypeName);
                sb.Append(" request, CancellationToken cancellationToken = default)");
                sb.AppendLine();
                sb.AppendLine("    {");
                sb.Append("        const string gqlQuery = @\"");
                sb.Append(gqlOperationType);
                sb.Append(' ');
                sb.Append(executeMethodName);
                if (variableDefinitions.Length > 0)
                {
                    sb.Append('(');
                    sb.Append(variableDefinitions);
                    sb.Append(')');
                }

                sb.Append(" { ");
                sb.Append(fieldName);
                if (fieldArguments.Length > 0)
                {
                    sb.Append('(');
                    sb.Append(fieldArguments);
                    sb.Append(')');
                }

                sb.Append(' ');
                if (!string.IsNullOrWhiteSpace(selectionSet))
                {
                    sb.Append(selectionSet);
                    sb.Append(' ');
                }

                sb.Append("}\";");
                sb.AppendLine();
                sb.Append("        return _graphQlClient.Execute<");
                sb.Append(wrapperTypeName);
                sb.Append(">(gqlQuery, request, cancellationToken);");
                sb.AppendLine();
                sb.AppendLine("    }");
                sb.AppendLine();
                AppendDescription(ref sb, $"Executes the operation and returns only the '{fieldName}' value from the GraphQL data envelope.", 1);
                sb.Append("    public async ValueTask<");
                sb.Append(nullableResultType);
                sb.Append("> GetValue(");
                sb.Append(requestTypeName);
                sb.Append(" request, CancellationToken cancellationToken = default)");
                sb.AppendLine();
                sb.AppendLine("    {");
                sb.Append("        GraphQlResponse<");
                sb.Append(wrapperTypeName);
                sb.Append("> response = await Execute(request, cancellationToken).ConfigureAwait(false);");
                sb.AppendLine();
                sb.Append("        return response.Data?.");
                sb.Append(wrapperPropertyName);
                sb.AppendLine(";");
                sb.AppendLine("    }");
            }
            else
            {
                AppendDescription(ref sb, $"Executes the GraphQL {gqlOperationType} '{fieldName}'.", 1);
                sb.Append("    public ValueTask<GraphQlResponse<");
                sb.Append(wrapperTypeName);
                sb.Append(">> Execute(CancellationToken cancellationToken = default)");
                sb.AppendLine();
                sb.AppendLine("    {");
                sb.Append("        const string gqlQuery = @\"");
                sb.Append(gqlOperationType);
                sb.Append(' ');
                sb.Append(executeMethodName);
                sb.Append(" { ");
                sb.Append(fieldName);
                if (!string.IsNullOrWhiteSpace(selectionSet))
                {
                    sb.Append(' ');
                    sb.Append(selectionSet);
                    sb.Append(' ');
                }

                sb.Append("}\";");
                sb.AppendLine();
                sb.AppendLine("        return _graphQlClient.Execute<" + wrapperTypeName + ">(gqlQuery, null, cancellationToken);");
                sb.AppendLine("    }");
                sb.AppendLine();
                AppendDescription(ref sb, $"Executes the operation and returns only the '{fieldName}' value from the GraphQL data envelope.", 1);
                sb.Append("    public async ValueTask<");
                sb.Append(nullableResultType);
                sb.Append("> GetValue(CancellationToken cancellationToken = default)");
                sb.AppendLine();
                sb.AppendLine("    {");
                sb.Append("        GraphQlResponse<");
                sb.Append(wrapperTypeName);
                sb.Append("> response = await Execute(cancellationToken).ConfigureAwait(false);");
                sb.AppendLine();
                sb.Append("        return response.Data?.");
                sb.Append(wrapperPropertyName);
                sb.AppendLine(";");
                sb.AppendLine("    }");
            }

            sb.AppendLine("}");
            return new GeneratedFile($"Clients/{pathSegment}/{requestBuilderName}.cs", sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }
    }

    private GeneratedFile GenerateOperationResponseWrapper(GraphQLFieldDefinition field, string wrapperTypeName, string pathSegment)
    {
        string fieldName = NameOf(field.Name);
        string fieldClrType = MapOutputType(field.Type);
        string propertyName = CSharpNaming.ToClrPropertyName(fieldName, wrapperTypeName);
        string? description = GetDescription(field.Description);
        var usings = CreateUsingSet(["System.Text.Json.Serialization"]);
        AddUsingsForType(usings, fieldClrType);
        var sb = new PooledStringBuilder();
        try
        {
        AppendHeader(ref sb, usings);
        AppendDescription(ref sb, $"Response data wrapper for the '{fieldName}' GraphQL operation.", 0);
        sb.Append("public sealed partial class ");
        sb.Append(wrapperTypeName);
        sb.AppendLine();
        sb.AppendLine("{");
        AppendDescription(ref sb, description, 1);
        sb.Append("    [JsonPropertyName(\"");
        sb.Append(fieldName);
        sb.AppendLine("\")]");
        sb.Append("    public ");
        sb.Append(fieldClrType);
        sb.Append(' ');
        sb.Append(propertyName);
        sb.Append(" { get; init; }");
        if (ShouldInitializeCollection(field.Type, fieldClrType))
            sb.Append(" = [];");
        else if (IsReferenceTypeNeedingNullForgiving(field.Type, fieldClrType))
            sb.Append(" = null!;");
        sb.AppendLine();
        sb.AppendLine("}");
        return new GeneratedFile($"Operations/{pathSegment}/{wrapperTypeName}.cs", sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }
    }

    private IReadOnlyList<GeneratedFile> GenerateGroupedOperationBuilderFiles(IReadOnlyList<OperationLayout> queryLayouts, IReadOnlyList<OperationLayout> mutationLayouts)
    {
        IReadOnlyList<OperationGroup> groups = GetOperationGroups(queryLayouts, mutationLayouts);

        if (groups.Count == 0)
            return [];

        var files = new List<GeneratedFile>();

        foreach (OperationGroup group in groups)
        {
            AddGroupedOperationBuilderFiles(group, files);
        }

        return files;
    }

    private void AddGroupedOperationBuilderFiles(OperationGroup group, ICollection<GeneratedFile> files)
    {
        files.Add(GenerateGroupedOperationBuilder(group));

        foreach (OperationGroup childGroup in group.ChildGroups)
        {
            AddGroupedOperationBuilderFiles(childGroup, files);
        }
    }

    private GeneratedFile GenerateGroupedOperationBuilder(OperationGroup group)
    {
        string builderName = CSharpNaming.ToOperationGroupBuilderName(group.FullName);
        var sb = new PooledStringBuilder();

        try
        {
            AppendHeader(ref sb);
            AppendDescription(ref sb, $"Groups GraphQL request builders for the '{CSharpNaming.ToCamelCase(group.FullName)}' resource.", 0);
            sb.Append("public sealed partial class ");
            sb.Append(builderName);
            sb.AppendLine();
            sb.AppendLine("{");
            sb.AppendLine("    private readonly IGraphQlClient _graphQlClient;");
            sb.AppendLine();
            sb.Append("    public ");
            sb.Append(builderName);
            sb.AppendLine("(IGraphQlClient graphQlClient)");
            sb.AppendLine("    {");
            sb.AppendLine("        _graphQlClient = graphQlClient;");
            sb.AppendLine("    }");
            sb.AppendLine();

            foreach (OperationGroup childGroup in group.ChildGroups)
            {
                string childBuilderName = CSharpNaming.ToOperationGroupBuilderName(childGroup.FullName);
                string propertyName = CSharpNaming.ToClrPropertyName(childGroup.SegmentName, builderName);
                sb.AppendLine("    /// <summary>");
                sb.Append("    /// Builds and executes grouped requests for the '");
                sb.Append(CSharpNaming.ToCamelCase(childGroup.FullName));
                sb.Append("' resource.</summary>");
                sb.AppendLine();
                sb.Append("    public ");
                sb.Append(childBuilderName);
                sb.Append(' ');
                sb.Append(propertyName);
                sb.Append(" => new ");
                sb.Append(childBuilderName);
                sb.AppendLine("(_graphQlClient);");
                sb.AppendLine();
            }

            foreach (GroupedOperation operation in group.Operations)
            {
                string propertyName = CSharpNaming.ToClrPropertyName(operation.ActionName, builderName);
                sb.AppendLine("    /// <summary>");
                sb.Append("    /// Builds and executes requests for the '");
                sb.Append(operation.FieldName);
                sb.Append("' ");
                sb.Append(operation.OperationKind.ToLowerInvariant());
                sb.Append(".</summary>");
                sb.AppendLine();
                sb.Append("    public ");
                sb.Append(operation.RequestBuilderName);
                sb.Append(' ');
                sb.Append(propertyName);
                sb.Append(" => new ");
                sb.Append(operation.RequestBuilderName);
                sb.AppendLine("(_graphQlClient);");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            return new GeneratedFile($"Clients/{string.Join("/", group.Segments)}/{builderName}.cs", sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }
    }

    private IReadOnlyList<OperationGroup> GetOperationGroups(IReadOnlyList<OperationLayout> queryLayouts, IReadOnlyList<OperationLayout> mutationLayouts)
    {
        var root = new OperationGroupNode(string.Empty);

        foreach (OperationLayout layout in queryLayouts.Concat(mutationLayouts).Where(static layout => layout.GroupSegments.Count > 0))
        {
            OperationGroupNode current = root;

            foreach (string segment in layout.GroupSegments)
            {
                current = current.GetOrAddChild(segment);
            }

            current.Operations.Add(new GroupedOperation(layout.OperationKind, layout.FieldName, layout.ActionName, layout.RequestBuilderName));
        }

        return BuildOperationGroups(root, []);
    }

    private IReadOnlyList<OperationGroup> BuildOperationGroups(OperationGroupNode node, IReadOnlyList<string> parentSegments)
    {
        if (node.Children.Count == 0)
            return [];

        var groups = new List<OperationGroup>(node.Children.Count);

        foreach ((string _, OperationGroupNode childNode) in node.Children)
        {
            List<string> segments = [.. parentSegments, childNode.SegmentName];
            IReadOnlyList<OperationGroup> childGroups = BuildOperationGroups(childNode, segments);
            string fullName = string.Concat(segments);

            groups.Add(new OperationGroup(
                childNode.SegmentName,
                fullName,
                segments,
                childNode.Operations.OrderBy(static operation => operation.ActionName, StringComparer.Ordinal).ToList(),
                childGroups));
        }

        return groups;
    }

    private IReadOnlyList<OperationLayout> GetOperationLayouts(GraphQLObjectTypeDefinition? rootType, string operationKind)
    {
        if (rootType?.Fields?.Items is not { Count: > 0 })
            return [];

        var operations = rootType.Fields.Items
            .Select(field =>
            {
                string fieldName = NameOf(field.Name);
                string clrName = CSharpNaming.ToClrTypeName(fieldName);

                return new OperationDescriptor(
                    fieldName,
                    clrName,
                    CSharpNaming.SplitPascalCaseTokens(clrName),
                    CSharpNaming.ToOperationRequestBuilderName(fieldName, operationKind));
            })
            .ToList();

        var root = new OperationTokenNode(string.Empty);

        foreach (OperationDescriptor operation in operations)
        {
            OperationTokenNode current = root;

            foreach (string token in operation.Tokens)
            {
                current = current.GetOrAddChild(token);
            }

            current.TerminalOperations.Add(operation);
        }

        root.ComputeDescendantOperationCount();

        var layouts = new List<OperationLayout>(operations.Count);
        BuildOperationLayouts(root, 0, [], operationKind, layouts);
        return layouts
            .OrderBy(static layout => layout.FieldName, StringComparer.Ordinal)
            .ToList();
    }

    private void BuildOperationLayouts(OperationTokenNode scopeNode, int scopeTokenCount, IReadOnlyList<string> groupSegments, string operationKind, ICollection<OperationLayout> layouts)
    {
        var groupedChildren = new Dictionary<string, CompressedGroupTarget>(StringComparer.Ordinal);

        foreach ((string _, OperationTokenNode childNode) in scopeNode.Children)
        {
            if (childNode.DescendantOperationCount < 2 || childNode.TerminalOperations.Count > 0)
                continue;

            OperationTokenNode current = childNode;
            int tokenLength = 1;
            var segmentTokens = new List<string> { childNode.SegmentName };

            while (current.TerminalOperations.Count == 0 && current.Children.Count == 1)
            {
                OperationTokenNode next = current.Children.Values.First();

                if (next.DescendantOperationCount < 2 || next.TerminalOperations.Count > 0)
                    break;

                current = next;
                tokenLength++;
                segmentTokens.Add(current.SegmentName);
            }

            groupedChildren[childNode.SegmentName] = new CompressedGroupTarget(current, string.Concat(segmentTokens), tokenLength);
        }

        foreach ((string _, CompressedGroupTarget target) in groupedChildren.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            List<string> childGroupSegments = [.. groupSegments, target.GroupName];
            BuildOperationLayouts(target.Node, scopeTokenCount + target.TokenLength, childGroupSegments, operationKind, layouts);
        }

        foreach (OperationDescriptor operation in CollectDirectOperations(scopeNode, groupedChildren.Keys))
        {
            string actionName = string.Concat(operation.Tokens.Skip(scopeTokenCount));
            string pathSegment = groupSegments.Count > 0 ? $"{string.Join("/", groupSegments)}/{actionName}" : operation.ClrName;
            layouts.Add(new OperationLayout(operationKind, operation.FieldName, operation.RequestBuilderName, pathSegment, groupSegments, actionName));
        }
    }

    private static IReadOnlyList<OperationDescriptor> CollectDirectOperations(OperationTokenNode scopeNode, IEnumerable<string> groupedChildNames)
    {
        var operations = new List<OperationDescriptor>();
        var groupedChildren = new HashSet<string>(groupedChildNames, StringComparer.Ordinal);
        CollectDirectOperations(scopeNode, groupedChildren, operations);
        return operations;
    }

    private static void CollectDirectOperations(OperationTokenNode node, IReadOnlySet<string> groupedChildNames, ICollection<OperationDescriptor> operations)
    {
        foreach (OperationDescriptor operation in node.TerminalOperations)
        {
            operations.Add(operation);
        }

        foreach ((string childName, OperationTokenNode childNode) in node.Children)
        {
            if (groupedChildNames.Contains(childName))
                continue;

            CollectAllOperations(childNode, operations);
        }
    }

    private static void CollectAllOperations(OperationTokenNode node, ICollection<OperationDescriptor> operations)
    {
        foreach (OperationDescriptor operation in node.TerminalOperations)
        {
            operations.Add(operation);
        }

        foreach ((string _, OperationTokenNode childNode) in node.Children)
        {
            CollectAllOperations(childNode, operations);
        }
    }

    private sealed record OperationDescriptor(string FieldName, string ClrName, IReadOnlyList<string> Tokens, string RequestBuilderName);
    private sealed record OperationLayout(string OperationKind, string FieldName, string RequestBuilderName, string PathSegment, IReadOnlyList<string> GroupSegments, string ActionName);
    private sealed record GroupedOperation(string OperationKind, string FieldName, string ActionName, string RequestBuilderName);
    private sealed record OperationGroup(string SegmentName, string FullName, IReadOnlyList<string> Segments, IReadOnlyList<GroupedOperation> Operations, IReadOnlyList<OperationGroup> ChildGroups);
    private sealed record CompressedGroupTarget(OperationTokenNode Node, string GroupName, int TokenLength);

    private sealed class OperationGroupNode
    {
        public OperationGroupNode(string segmentName)
        {
            SegmentName = segmentName;
        }

        public string SegmentName { get; }
        public SortedDictionary<string, OperationGroupNode> Children { get; } = new(StringComparer.Ordinal);
        public List<GroupedOperation> Operations { get; } = [];

        public OperationGroupNode GetOrAddChild(string segmentName)
        {
            if (!Children.TryGetValue(segmentName, out OperationGroupNode? child))
            {
                child = new OperationGroupNode(segmentName);
                Children[segmentName] = child;
            }

            return child;
        }
    }

    private sealed class OperationTokenNode
    {
        public OperationTokenNode(string segmentName)
        {
            SegmentName = segmentName;
        }

        public string SegmentName { get; }
        public SortedDictionary<string, OperationTokenNode> Children { get; } = new(StringComparer.Ordinal);
        public List<OperationDescriptor> TerminalOperations { get; } = [];
        public int DescendantOperationCount { get; private set; }

        public OperationTokenNode GetOrAddChild(string segmentName)
        {
            if (!Children.TryGetValue(segmentName, out OperationTokenNode? child))
            {
                child = new OperationTokenNode(segmentName);
                Children[segmentName] = child;
            }

            return child;
        }

        public int ComputeDescendantOperationCount()
        {
            int count = TerminalOperations.Count;

            foreach ((string _, OperationTokenNode childNode) in Children)
            {
                count += childNode.ComputeDescendantOperationCount();
            }

            DescendantOperationCount = count;
            return count;
        }
    }

    private string GetEntryClientTypeName()
    {
        string configuredName = _config.EntryClientName;

        if (string.IsNullOrWhiteSpace(configuredName))
            configuredName = "GraphQlClient";

        return CSharpNaming.ToClrTypeName(configuredName);
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
