using Soenneker.Utils.PooledStringBuilders;
using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates the JsonSerializerContext for source-generated serialization. Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private GeneratedFile GenerateJsonContext(GraphQLDocument document)
    {
        var allTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach (ASTNode definition in document.Definitions)
        {
            switch (definition)
            {
                case GraphQLObjectTypeDefinition obj:
                    if (!IsBuiltInRootType(NameOf(obj.Name)) || _config.EmitRootTypes)
                        allTypes.Add(CSharpNaming.ToClrTypeName(NameOf(obj.Name)));
                    break;
                case GraphQLInputObjectTypeDefinition input:
                    allTypes.Add(CSharpNaming.ToClrTypeName(NameOf(input.Name)));
                    break;
                case GraphQLEnumTypeDefinition enm:
                    allTypes.Add(CSharpNaming.ToClrTypeName(NameOf(enm.Name)));
                    break;
            }
        }

        allTypes.Add("GraphQlError");
        allTypes.Add("GraphQlErrorLocation");

        GraphQLObjectTypeDefinition? queryRoot = FindObjectType(_config.QueryRootTypeName);
        GraphQLObjectTypeDefinition? mutationRoot = FindObjectType(_config.MutationRootTypeName);

        if (queryRoot?.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in queryRoot.Fields.Items)
                allTypes.Add(CSharpNaming.ToOperationDataTypeName(NameOf(field.Name), "Query"));
        }

        if (mutationRoot?.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in mutationRoot.Fields.Items)
                allTypes.Add(CSharpNaming.ToOperationDataTypeName(NameOf(field.Name), "Mutation"));
        }

        var sb = new PooledStringBuilder();
        try
        {
        AppendHeader(ref sb);
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine("[JsonSourceGenerationOptions(");
        sb.AppendLine("    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,");
        sb.AppendLine("    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,");
        sb.AppendLine("    WriteIndented = true)]");
        foreach (string type in allTypes.OrderBy(static x => x, StringComparer.Ordinal))
        {
            sb.Append("[JsonSerializable(typeof(");
            sb.Append(type);
            sb.AppendLine("))]");
            sb.Append("[JsonSerializable(typeof(GraphQlResponse<");
            sb.Append(type);
            sb.AppendLine(">))]");
        }
        sb.Append("public partial class ");
        sb.Append(_config.JsonSerializerContextName);
        sb.Append(" : JsonSerializerContext");
        sb.AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("}");
        return new GeneratedFile($"Transport/{_config.JsonSerializerContextName}.cs", sb.ToString());
        }
        finally
        {
            sb.Dispose();
        }
    }
}
