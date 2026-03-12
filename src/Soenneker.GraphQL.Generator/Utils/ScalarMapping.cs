using Soenneker.GraphQL.Generator.Config;

namespace Soenneker.GraphQL.Generator.Utils;

/// <summary>
/// Builds and applies GraphQL scalar → CLR type mappings.
/// </summary>
internal static class ScalarMapping
{
    public static IReadOnlyDictionary<string, string> CreateScalarMap(GeneratorConfig config)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["String"] = "string",
            ["ID"] = config.IdClrType,
            ["Int"] = "int",
            ["Float"] = "double",
            ["Boolean"] = "bool",

            ["Date"] = "DateOnly",
            ["DateTime"] = "DateTimeOffset",
            ["DateTimeUtc"] = "DateTimeOffset",
            ["Time"] = "TimeOnly",
            ["Decimal"] = "decimal",
            ["UUID"] = "Guid",
            ["Guid"] = "Guid",
            ["Long"] = "long",
            ["Short"] = "short",
            ["Byte"] = "byte",
            ["Any"] = "object",
            ["JSON"] = "string",
            ["JSONObject"] = "string",
            ["URI"] = "Uri",
            ["Url"] = "Uri",
            ["URL"] = "Uri",

            ["HTML"] = "string",
            ["FormattedString"] = "string",
            ["UnsignedInt64"] = "ulong",
            ["BigInt"] = "long",
            ["StorefrontID"] = "string",
            ["Color"] = "string",
            ["UtcOffset"] = "string",
            ["ARN"] = "string"
        };

        if (config.ScalarMappings is not null)
        {
            foreach ((string key, string value) in config.ScalarMappings)
            {
                map[key] = value;
            }
        }

        return map;
    }

    public static bool IsNonNullableValueType(string clrType)
    {
        return clrType is
            "bool" or
            "byte" or
            "sbyte" or
            "short" or
            "ushort" or
            "int" or
            "uint" or
            "long" or
            "ulong" or
            "nint" or
            "nuint" or
            "float" or
            "double" or
            "decimal" or
            "char" or
            "Guid" or
            "DateOnly" or
            "TimeOnly" or
            "DateTime" or
            "DateTimeOffset" or
            "TimeSpan";
    }
}
