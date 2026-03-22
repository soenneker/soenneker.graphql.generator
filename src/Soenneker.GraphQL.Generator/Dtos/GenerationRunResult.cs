namespace Soenneker.GraphQL.Generator.Dtos;

/// <summary>
/// Result of a generation run that writes files to disk.
/// </summary>
/// <param name="OutputDirectory">Resolved output directory where files were written.</param>
/// <param name="Result">Underlying generation result.</param>
public sealed record GenerationRunResult(string OutputDirectory, GenerationResult Result);
