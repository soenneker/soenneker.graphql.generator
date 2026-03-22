using System.Text;
using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Dtos;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates C# type definitions (objects, inputs, enums, interfaces, unions, scalars). Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private GeneratedFile GenerateObjectType(GraphQLObjectTypeDefinition obj)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(obj.Name));
        string? description = GetDescription(obj.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);

        var interfaces = obj.Interfaces?.Items?
            .Select(i => CSharpNaming.ToClrTypeName(NameOf(i.Name)))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? [];
        string inheritance = interfaces.Count > 0 ? " : " + string.Join(", ", interfaces) : string.Empty;

        AppendDescription(sb, description, 0);
        sb.Append("public sealed partial class ").Append(typeName).Append(inheritance).AppendLine();
        sb.AppendLine("{");

        if (obj.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in obj.Fields.Items)
            {
                string propertyType = MapOutputType(field.Type);
                string propertyName = CSharpNaming.ToClrPropertyName(NameOf(field.Name), typeName);
                string? fieldDescription = GetDescription(field.Description);
                AppendDescription(sb, fieldDescription, 1);
                sb.Append("    public ").Append(propertyType).Append(' ').Append(propertyName).Append(" { get; init; }");
                if (ShouldInitializeCollection(field.Type, propertyType))
                    sb.Append(" = [];");
                else if (IsReferenceTypeNeedingNullForgiving(field.Type, propertyType))
                    sb.Append(" = null!;");
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return new GeneratedFile($"Types/Objects/{typeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateInputType(GraphQLInputObjectTypeDefinition input)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(input.Name));
        string? description = GetDescription(input.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, description, 0);
        sb.Append("public sealed partial class ").Append(typeName).AppendLine();
        sb.AppendLine("{");

        if (input.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLInputValueDefinition field in input.Fields.Items)
            {
                string propertyType = MapInputType(field.Type);
                string propertyName = CSharpNaming.ToClrPropertyName(NameOf(field.Name), typeName);
                string? fieldDescription = GetDescription(field.Description);
                AppendDescription(sb, fieldDescription, 1);
                sb.Append("    public ").Append(propertyType).Append(' ').Append(propertyName).Append(" { get; init; }");
                if (ShouldInitializeCollection(field.Type, propertyType))
                    sb.Append(" = [];");
                else if (IsReferenceTypeNeedingNullForgiving(field.Type, propertyType))
                    sb.Append(" = null!;");
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return new GeneratedFile($"Types/Inputs/{typeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateEnumType(GraphQLEnumTypeDefinition enm)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(enm.Name));
        string? description = GetDescription(enm.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, description, 0);
        sb.Append("public enum ").Append(typeName).AppendLine();
        sb.AppendLine("{");

        if (enm.Values?.Items is { Count: > 0 })
        {
            for (int i = 0; i < enm.Values.Items.Count; i++)
            {
                GraphQLEnumValueDefinition value = enm.Values.Items[i];
                string memberName = CSharpNaming.ToClrEnumMemberName(NameOf(value.Name));
                string? valueDescription = GetDescription(value.Description);
                AppendDescription(sb, valueDescription, 1);
                sb.Append("    ").Append(memberName);
                if (i < enm.Values.Items.Count - 1)
                    sb.Append(',');
                sb.AppendLine();
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return new GeneratedFile($"Types/Enums/{typeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateInterfaceType(GraphQLInterfaceTypeDefinition iface)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(iface.Name));
        string? description = GetDescription(iface.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, description, 0);
        sb.Append("public interface ").Append(typeName).AppendLine();
        sb.AppendLine("{");

        if (iface.Fields?.Items is { Count: > 0 })
        {
            foreach (GraphQLFieldDefinition field in iface.Fields.Items)
            {
                string propertyType = MapOutputType(field.Type);
                string propertyName = CSharpNaming.ToClrPropertyName(NameOf(field.Name), typeName);
                string? fieldDescription = GetDescription(field.Description);
                AppendDescription(sb, fieldDescription, 1);
                sb.Append("    ").Append(propertyType).Append(' ').Append(propertyName).Append(" { get; }").AppendLine();
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");
        return new GeneratedFile($"Types/Interfaces/{typeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateUnionType(GraphQLUnionTypeDefinition union)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(union.Name));
        string? description = GetDescription(union.Description);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, description, 0);
        sb.Append("public interface ").Append(typeName).AppendLine();
        sb.AppendLine("{");
        sb.AppendLine("}");
        return new GeneratedFile($"Types/Unions/{typeName}.cs", sb.ToString());
    }

    private GeneratedFile GenerateScalarAlias(GraphQLScalarTypeDefinition scalar)
    {
        string typeName = CSharpNaming.ToClrTypeName(NameOf(scalar.Name));
        string? description = GetDescription(scalar.Description);
        string mappedType = MapNamedType(NameOf(scalar.Name), nullable: false);
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendDescription(sb, description, 0);
        sb.Append("global using ").Append(typeName).Append(" = ").Append(mappedType).AppendLine(";");
        return new GeneratedFile($"Types/Scalars/{typeName}.global.cs", sb.ToString());
    }
}
