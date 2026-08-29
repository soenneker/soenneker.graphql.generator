namespace Soenneker.GraphQL.Generator.Dtos;

public sealed record GenerationResult(
    List<GeneratedFile> Files,
    int ObjectCount,
    int InputCount,
    int EnumCount,
    int InterfaceCount,
    int UnionCount,
    int ScalarCount,
    int OperationFileCount);
