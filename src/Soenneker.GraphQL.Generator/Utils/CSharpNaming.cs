using System.Text.RegularExpressions;
using Soenneker.Utils.PooledStringBuilders;

namespace Soenneker.GraphQL.Generator.Utils;

/// <summary>
/// Helpers for converting GraphQL names to valid C# identifiers and PascalCase.
/// </summary>
internal static class CSharpNaming
{
    private static readonly Regex _pascalCaseTokenRegex = new("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|\\d+", RegexOptions.Compiled);
    private static readonly HashSet<string> _cSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    public static string ToClrTypeName(string name)
    {
        string sanitized = SanitizeIdentifier(name);
        return ToPascalCase(sanitized);
    }

    public static string ToClrPropertyName(string name, string containingTypeName)
    {
        string sanitized = SanitizeIdentifier(name);
        string propertyName = ToPascalCase(sanitized);

        if (string.Equals(propertyName, containingTypeName, StringComparison.Ordinal))
            propertyName += "Value";

        return propertyName;
    }

    public static string ToClrEnumMemberName(string name)
    {
        string sanitized = SanitizeIdentifier(name);

        if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            sanitized = "_" + sanitized;

        return ToPascalCase(sanitized);
    }

    public static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "_";

        var sb = new PooledStringBuilder(value.Length);

        try
        {
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            string result = sb.ToString();

            if (result.Length == 0)
                return "_";

            if (char.IsDigit(result[0]))
                result = "_" + result;

            if (_cSharpKeywords.Contains(result))
                result = "@" + result;

            return result;
        }
        finally
        {
            sb.Dispose();
        }
    }

    public static string ToPascalCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        bool hasAtPrefix = value[0] == '@';
        string core = hasAtPrefix ? value[1..] : value;

        string[] parts = core.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return hasAtPrefix ? "@_" : "_";

        var sb = new PooledStringBuilder(core.Length);

        try
        {
            foreach (string part in parts)
            {
                if (part.Length == 0)
                    continue;

                if (part.Length == 1)
                {
                    sb.Append(char.ToUpperInvariant(part[0]));
                    continue;
                }

                sb.Append(char.ToUpperInvariant(part[0]));
                sb.Append(part.AsSpan(1));
            }

            string result = sb.ToString();
            return hasAtPrefix ? "@" + result : result;
        }
        finally
        {
            sb.Dispose();
        }
    }

    public static bool IsReservedKeyword(string value) => _cSharpKeywords.Contains(value);

    /// <summary>
    /// Returns a C#-safe parameter/variable name (with @ prefix when the name is a reserved keyword).
    /// Use when emitting method parameters and variable initializers so names like "namespace" become "@namespace".
    /// </summary>
    public static string SafeParameterName(string camelCaseName)
    {
        if (string.IsNullOrEmpty(camelCaseName)) return camelCaseName;
        return _cSharpKeywords.Contains(camelCaseName) ? "@" + camelCaseName : camelCaseName;
    }

    public static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value[0] == '@')
            value = value[1..];

        if (value.Length == 1)
            return char.ToLowerInvariant(value[0]).ToString();

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    public static string ToOperationMethodName(string fieldName, string operationKind)
    {
        string clr = ToClrTypeName(fieldName);

        if (operationKind.Equals("Query", StringComparison.Ordinal))
            return "Get" + clr;

        if (operationKind.Equals("Mutation", StringComparison.Ordinal))
            return clr;

        return clr;
    }

    public static string ToOperationDataTypeName(string fieldName, string operationKind)
    {
        string methodName = ToOperationMethodName(fieldName, operationKind);
        return methodName + "Data";
    }

    public static string ToOperationValueMethodName(string fieldName, string operationKind)
    {
        string methodName = ToOperationMethodName(fieldName, operationKind);
        return methodName + "Value";
    }

    /// <summary>
    /// Returns the request builder type name for an operation (e.g. GetUserRequestBuilder, CreateUserRequestBuilder).
    /// </summary>
    public static string ToOperationRequestBuilderName(string fieldName, string operationKind)
    {
        string clr = ToClrTypeName(fieldName);
        if (operationKind.Equals("Query", StringComparison.Ordinal))
            return "Get" + clr + "RequestBuilder";
        return clr + "RequestBuilder";
    }

    /// <summary>
    /// Returns the variables/request type name for an operation (e.g. GetUserVariables, CustomerMergeVariables).
    /// Uses "Variables" suffix to avoid clashing with schema types like CustomerMergeRequest.
    /// </summary>
    public static string ToOperationRequestName(string fieldName, string operationKind)
    {
        string clr = ToClrTypeName(fieldName);
        if (operationKind.Equals("Query", StringComparison.Ordinal))
            return "Get" + clr + "Variables";
        return clr + "Variables";
    }

    /// <summary>
    /// Returns the property name on GraphQlClient (e.g. GetUser, CreateUser).
    /// </summary>
    public static string ToOperationBuilderPropertyName(string fieldName, string operationKind)
    {
        string clr = ToClrTypeName(fieldName);
        if (operationKind.Equals("Query", StringComparison.Ordinal))
            return "Get" + clr;
        return clr;
    }

    public static string ToOperationGroupBuilderName(string resourceName) => resourceName + "Builder";

    public static IReadOnlyList<string> SplitPascalCaseTokens(string value)
    {
        var tokens = new List<string>();

        foreach (Match match in _pascalCaseTokenRegex.Matches(value))
        {
            if (match.Success && match.Length > 0)
                tokens.Add(match.Value);
        }

        return tokens;
    }
}
