[![](https://img.shields.io/nuget/v/soenneker.graphql.generator.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.graphql.generator/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.graphql.generator/build-and-test.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.graphql.generator/actions/workflows/build-and-test.yml)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.graphql.generator/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.graphql.generator/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.graphql.generator.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.graphql.generator/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.graphql.generator/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.graphql.generator/actions/workflows/codeql.yml)

# Soenneker.GraphQL.Generator

Generates C# schema models, operation builders, selection sets, JSON serialization metadata, and an HTTP transport from GraphQL SDL.

## Install

```bash
dotnet add package Soenneker.GraphQL.Generator
```

## Generate in memory

Use the parameterless generator when the caller will inspect or write the generated files:

```csharp
var config = new GeneratorConfig
{
    Namespace = "MyCompany.MyApi.GraphQL",
    OutputDirectory = "./Generated",
    EntryClientName = "MyApiClient"
};

var generator = new GraphQlGenerator();
GenerationResult result = generator.Generate(schemaSdl, config);

foreach (GeneratedFile file in result.Files)
    Console.WriteLine(file.RelativePath);
```

`Generate()` does not access the filesystem; `OutputDirectory` is ignored by that method.

## Generate files with DI

```csharp
services.AddGraphQlGeneratorAsScoped();

GenerationRunResult run = await generator.Run(
    schemaSdl,
    config,
    cancellationToken);
```

`Run()` writes each generated file beneath `OutputDirectory` and returns its resolved absolute path with the generation counts. Existing files at generated paths are overwritten. Files no longer produced by a changed schema are not deleted, so use a dedicated output directory and clean it explicitly when stale output must be removed.

Singleton registration is also available through `AddGraphQlGeneratorAsSingleton()`.

## Generate from a config file

```json
{
  "SchemaPath": "./schema.graphql",
  "OutputDirectory": "./Generated",
  "Namespace": "MyCompany.MyApi.GraphQL",
  "EntryClientName": "MyApiClient",
  "IdClrType": "string",
  "EmitRootTypes": true,
  "EmitScalarAliases": false,
  "EmitJsonSerializerContext": true,
  "JsonSerializerContextName": "MyApiJsonContext",
  "EmitOperationClients": true,
  "MaxSelectionDepth": 2
}
```

```csharp
GenerationRunResult run = await generator.RunFromConfig(
    "./gql2cs.json",
    cancellationToken);
```

Relative `SchemaPath` and `OutputDirectory` values are resolved from the process working directory.

The repository also contains a CLI project. After building or publishing `cli/Soenneker.GraphQL.Generator.Cli`, run:

```bash
gql2cs --config ./gql2cs.json
```

## Generated client usage

The output includes `GraphQlHttpClient`, `IGraphQlClient`, transport DTOs, and the configured entry client:

```csharp
var httpClient = new HttpClient
{
    BaseAddress = new Uri("https://api.example.com/graphql")
};

var transport = new GraphQlHttpClient(httpClient);
var client = new MyApiClient(transport);
```

Generated operation builders create query documents and variables from the SDL. HTTP errors throw through `EnsureSuccessStatusCode`; GraphQL errors in a successful HTTP response remain in `GraphQlResponse<T>.Errors` for the caller to inspect.

## Important options

| Option | Effect |
| --- | --- |
| `ScalarMappings` | Overrides GraphQL scalar-to-CLR type mappings. |
| `EmitScalarAliases` | Emits global aliases for otherwise unmapped custom scalars. |
| `EmitOperationClients` | Emits request builders and the entry client for query and mutation fields. |
| `MaxSelectionDepth` | Limits automatic nested selection-set expansion. |
| `GlobalUsings` | Adds namespace imports to generated files. |
| `EmitJsonSerializerContext` | Emits the configured source-generated JSON context. |

Schema parsing and in-memory generation are synchronous. Cancellation applies to filesystem reads, directory creation, and file writes performed by `Run()` and `RunFromConfig()`.
