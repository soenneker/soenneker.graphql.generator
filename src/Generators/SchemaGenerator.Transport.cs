using System.Text;
using Soenneker.GraphQL.Generator.Models;

namespace Soenneker.GraphQL.Generator.Generators;

/// <summary>
/// Generates transport types (request, response, error, client interfaces). Part of <see cref="SchemaGenerator"/>.
/// </summary>
internal sealed partial class SchemaGenerator
{
    private GeneratedFile GenerateGraphQlRequestFile()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine("public sealed class GraphQlRequest");
        sb.AppendLine("{");
        sb.AppendLine("    [JsonPropertyName(\"query\")]");
        sb.AppendLine("    public required string Query { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"variables\")]");
        sb.AppendLine("    public object? Variables { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"operationName\")]");
        sb.AppendLine("    public string? OperationName { get; init; }");
        sb.AppendLine("}");
        return new GeneratedFile("Transport/GraphQlRequest.cs", sb.ToString());
    }

    private GeneratedFile GenerateGraphQlErrorFile()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine("public sealed class GraphQlError");
        sb.AppendLine("{");
        sb.AppendLine("    [JsonPropertyName(\"message\")]");
        sb.AppendLine("    public string? Message { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"path\")]");
        sb.AppendLine("    public List<object>? Path { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"extensions\")]");
        sb.AppendLine("    public Dictionary<string, object>? Extensions { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"locations\")]");
        sb.AppendLine("    public List<GraphQlErrorLocation>? Locations { get; init; }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public sealed class GraphQlErrorLocation");
        sb.AppendLine("{");
        sb.AppendLine("    [JsonPropertyName(\"line\")]");
        sb.AppendLine("    public int Line { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"column\")]");
        sb.AppendLine("    public int Column { get; init; }");
        sb.AppendLine("}");
        return new GeneratedFile("Transport/GraphQlError.cs", sb.ToString());
    }

    private GeneratedFile GenerateGraphQlResponseFile()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine("public sealed class GraphQlResponse<T>");
        sb.AppendLine("{");
        sb.AppendLine("    [JsonPropertyName(\"data\")]");
        sb.AppendLine("    public T? Data { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    [JsonPropertyName(\"errors\")]");
        sb.AppendLine("    public List<GraphQlError>? Errors { get; init; }");
        sb.AppendLine();
        sb.AppendLine("    public bool HasErrors => Errors is { Count: > 0 };");
        sb.AppendLine("}");
        return new GeneratedFile("Transport/GraphQlResponse.cs", sb.ToString());
    }

    private GeneratedFile GenerateIGraphQlClientFile()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("public interface IGraphQlClient");
        sb.AppendLine("{");
        sb.AppendLine("    Task<GraphQlResponse<T>> ExecuteAsync<T>(");
        sb.AppendLine("        string query,");
        sb.AppendLine("        object? variables = null,");
        sb.AppendLine("        CancellationToken cancellationToken = default);");
        sb.AppendLine("}");
        return new GeneratedFile("Transport/IGraphQlClient.cs", sb.ToString());
    }

    private GeneratedFile GenerateGraphQlHttpClientFile()
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Net.Http;");
        sb.AppendLine("using System.Net.Http.Json;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Threading;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();
        sb.AppendLine("public sealed class GraphQlHttpClient : IGraphQlClient");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly HttpClient _httpClient;");
        sb.AppendLine("    private readonly JsonSerializerOptions _serializerOptions;");
        sb.AppendLine();
        sb.AppendLine("    public GraphQlHttpClient(HttpClient httpClient, JsonSerializerOptions? serializerOptions = null)");
        sb.AppendLine("    {");
        sb.AppendLine("        _httpClient = httpClient;");
        sb.AppendLine("        _serializerOptions = serializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public async Task<GraphQlResponse<T>> ExecuteAsync<T>(");
        sb.AppendLine("        string query,");
        sb.AppendLine("        object? variables = null,");
        sb.AppendLine("        CancellationToken cancellationToken = default)");
        sb.AppendLine("    {");
        sb.AppendLine("        var request = new GraphQlRequest");
        sb.AppendLine("        {");
        sb.AppendLine("            Query = query,");
        sb.AppendLine("            Variables = variables");
        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(string.Empty, request, _serializerOptions, cancellationToken).ConfigureAwait(false);");
        sb.AppendLine("        response.EnsureSuccessStatusCode();");
        sb.AppendLine();
        sb.AppendLine("        GraphQlResponse<T>? payload = await response.Content.ReadFromJsonAsync<GraphQlResponse<T>>(_serializerOptions, cancellationToken).ConfigureAwait(false);");
        sb.AppendLine("        return payload ?? throw new InvalidOperationException(\"GraphQL response body was null.\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return new GeneratedFile("Transport/GraphQlHttpClient.cs", sb.ToString());
    }
}
