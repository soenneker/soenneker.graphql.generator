namespace Soenneker.GraphQL.Generator.Models;

/// <summary>
/// A single generated C# file with relative path and content.
/// </summary>
/// <param name="RelativePath">Path relative to the output directory (e.g. "MyType.cs").</param>
/// <param name="Content">Full file content.</param>
public sealed record GeneratedFile(string RelativePath, string Content);
