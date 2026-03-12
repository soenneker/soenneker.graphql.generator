using GraphQLParser;
using GraphQLParser.AST;
using Soenneker.GraphQL.Generator.Abstract;
using Soenneker.GraphQL.Generator.Config;
using Soenneker.GraphQL.Generator.Generators;
using Soenneker.GraphQL.Generator.Models;

namespace Soenneker.GraphQL.Generator;

/// <inheritdoc cref="IGraphQLGenerator"/>
public sealed class GraphQLGenerator : IGraphQLGenerator
{
    /// <inheritdoc />
    public GenerationResult Generate(string schemaContent, GeneratorConfig config)
    {
        GraphQLDocument document = Parser.Parse(schemaContent);
        var schemaGenerator = new SchemaGenerator(config, document);
        return schemaGenerator.Generate(document);
    }
}
