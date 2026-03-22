[![](https://img.shields.io/nuget/v/soenneker.graphql.generator.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.graphql.generator/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.graphql.generator/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.graphql.generator/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/soenneker.graphql.generator.svg?style=for-the-badge)](https://www.nuget.org/packages/soenneker.graphql.generator/)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.GraphQL.Generator
### A command line tool for generating API clients from GraphQL schemas

## Installation

```
dotnet add package Soenneker.GraphQL.Generator
```

## Usage

**Library:** Inject `IGraphQLGenerator` and either:

- call `Generate(schemaContent, config)` to get a `GenerationResult` with generated C# files and type counts
- call `Generate(schemaContent, config, cancellationToken)` to generate from an in-memory SDL string and write all files to `config.OutputDirectory`

**CLI (gql2cs):** Build and run the `Soenneker.GraphQL.Generator.Cli` project, or run the published `gql2cs` executable:

```bash
gql2cs --config ./gql2cs.json
```

### Library example

```csharp
var config = new GeneratorConfig
{
    Namespace = "MyCompany.MyApi.GraphQL",
    EntryClientName = "MyApiClient",
    OutputDirectory = "./Generated"
};

await graphQlGenerator.Generate(schemaSdl, config, cancellationToken);
```

The generated entry client is written to the output root as `<EntryClientName>.cs` rather than under the `Clients` folder.