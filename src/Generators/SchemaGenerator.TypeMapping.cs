using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Utils;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// GraphQL-to-C# type mapping and resolution. Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private string MapOutputType(GraphQLType type) => MapType(type, inputMode: false);
    private string MapInputType(GraphQLType type) => MapType(type, inputMode: true);

    private string MapType(GraphQLType type, bool inputMode)
    {
        return type switch
        {
            GraphQLNonNullType nonNull => MapNonNullType(nonNull.Type, inputMode),
            GraphQLListType list => $"List<{MapType(list.Type, inputMode).TrimEnd('?')}>?",
            GraphQLNamedType named => MapNamedType(NameOf(named.Name), nullable: true),
            _ => "object?"
        };
    }

    private string MapNonNullType(GraphQLType type, bool inputMode)
    {
        return type switch
        {
            GraphQLListType list => $"List<{MapType(list.Type, inputMode).TrimEnd('?')}>",
            GraphQLNamedType named => MapNamedType(NameOf(named.Name), nullable: false),
            GraphQLNonNullType nested => MapNonNullType(nested.Type, inputMode),
            _ => "object"
        };
    }

    private static string MapGraphQlInputTypeStatic(GraphQLType type)
    {
        return type switch
        {
            GraphQLNonNullType nonNull => MapGraphQlInputNonNullTypeStatic(nonNull.Type),
            GraphQLListType list => $"List<{MapGraphQlInputTypeStatic(list.Type).TrimEnd('?')}>?",
            GraphQLNamedType named => MapKnownScalarToClr(NameOf(named.Name), nullable: true),
            _ => "object?"
        };
    }

    private static string MapGraphQlInputNonNullTypeStatic(GraphQLType type)
    {
        return type switch
        {
            GraphQLListType list => $"List<{MapGraphQlInputTypeStatic(list.Type).TrimEnd('?')}>",
            GraphQLNamedType named => MapKnownScalarToClr(NameOf(named.Name), nullable: false),
            GraphQLNonNullType nested => MapGraphQlInputNonNullTypeStatic(nested.Type),
            _ => "object"
        };
    }

    private static string MapKnownScalarToClr(string typeName, bool nullable)
    {
        string clrType = typeName switch
        {
            "String" => "string",
            "ID" => "string",
            "Int" => "int",
            "Float" => "double",
            "Boolean" => "bool",
            "Date" => "DateOnly",
            "DateTime" => "DateTimeOffset",
            "DateTimeUtc" => "DateTimeOffset",
            "Time" => "TimeOnly",
            "Decimal" => "decimal",
            "UUID" => "Guid",
            "Guid" => "Guid",
            "Long" => "long",
            "Short" => "short",
            "Byte" => "byte",
            "Any" => "object",
            "JSON" => "string",
            "JSONObject" => "string",
            "URI" => "string",
            "Url" => "string",
            "URL" => "string",
            "HTML" => "string",
            "FormattedString" => "string",
            "UnsignedInt64" => "ulong",
            "BigInt" => "long",
            "StorefrontID" => "string",
            "Color" => "string",
            "UtcOffset" => "string",
            "ARN" => "string",
            _ => CSharpNaming.ToClrTypeName(typeName)
        };
        if (!nullable) return clrType;
        if (ScalarMapping.IsNonNullableValueType(clrType)) return clrType + '?';
        return clrType + '?';
    }

    private static string ToGraphQlTypeString(GraphQLType type)
    {
        return type switch
        {
            GraphQLNonNullType nonNull => ToGraphQlTypeString(nonNull.Type) + "!",
            GraphQLListType list => "[" + ToGraphQlTypeString(list.Type) + "]",
            GraphQLNamedType named => NameOf(named.Name),
            _ => "String"
        };
    }

    private static string GetNamedType(GraphQLType type)
    {
        return type switch
        {
            GraphQLNonNullType nonNull => GetNamedType(nonNull.Type),
            GraphQLListType list => GetNamedType(list.Type),
            GraphQLNamedType named => NameOf(named.Name),
            _ => "Object"
        };
    }

    private string MapNamedType(string graphQlTypeName, bool nullable)
    {
        string clrType = _scalarMap.TryGetValue(graphQlTypeName, out string? mapped)
            ? mapped
            : _definedScalars.Contains(graphQlTypeName)
                ? "string"
                : CSharpNaming.ToClrTypeName(graphQlTypeName);

        if (!nullable)
            return clrType;
        if (ScalarMapping.IsNonNullableValueType(clrType) || _definedEnums.Contains(clrType))
            return clrType + '?';
        return clrType + '?';
    }

    private static bool ShouldInitializeCollection(GraphQLType type, string propertyType)
        => propertyType.StartsWith("List<", StringComparison.Ordinal) && type is GraphQLNonNullType;

    private bool IsReferenceTypeNeedingNullForgiving(GraphQLType type, string propertyType)
    {
        if (type is not GraphQLNonNullType) return false;
        if (propertyType.StartsWith("List<", StringComparison.Ordinal)) return false;
        if (propertyType.EndsWith("?", StringComparison.Ordinal)) return false;
        if (ScalarMapping.IsNonNullableValueType(propertyType)) return false;
        if (_definedEnums.Contains(propertyType)) return false;
        return true;
    }

    private bool IsScalarOrEnum(string graphQlTypeName) =>
        _scalarMap.ContainsKey(graphQlTypeName) || _definedScalars.Contains(graphQlTypeName) || _definedEnums.Contains(CSharpNaming.ToClrTypeName(graphQlTypeName));
}
