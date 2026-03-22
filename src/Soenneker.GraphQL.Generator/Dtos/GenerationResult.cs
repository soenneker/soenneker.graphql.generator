namespace Soenneker.GraphQL.Generator.Dtos;

/// <summary>
/// Result of a GraphQL schema generation run: generated files and type counts.
/// </summary>
/// <param name="Files">Generated files to write to disk.</param>
/// <param name="ObjectCount">Number of object types emitted.</param>
/// <param name="InputCount">Number of input types emitted.</param>
/// <param name="EnumCount">Number of enums emitted.</param>
/// <param name="InterfaceCount">Number of interfaces emitted.</param>
/// <param name="UnionCount">Number of union types emitted.</param>
/// <param name="ScalarCount">Number of scalars seen.</param>
/// <param name="OperationFileCount">Number of operation client/wrapper files emitted.</param>
public sealed record GenerationResult(
    List<GeneratedFile> Files,
    int ObjectCount,
    int InputCount,
    int EnumCount,
    int InterfaceCount,
    int UnionCount,
    int ScalarCount,
    int OperationFileCount);
